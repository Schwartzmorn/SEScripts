using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
partial class Program {
  public class ArmController: JobProvider {
    const string SECTION = "arm-positions";
    readonly List<ArmRotor> _rotors;
    readonly Dictionary<string, ArmPos> _pos;
    readonly ArmAutoControl _autoCont;

    public float Angle => this._rotors[0].Angle;

    // TODO correct this
    public float TargetAngle => this._autoCont.Target.Angle;

    public ArmController(Ini ini, MyGridProgram p, CmdLine cmd, IMyShipController cont, WheelsController wCont) {
      var rotors = new List<IMyMotorStator>();
      p.GridTerminalSystem.GetBlocksOfType(rotors, r => r.CubeGrid == p.Me.CubeGrid && r.DisplayNameText.Contains("Arm Rotor"));
        this._rotors = rotors.Select(r => new ArmRotor(r, r.WorldMatrix.Up.Dot(cont.WorldMatrix.Right) > 0)).ToList();
      var keys = new List<MyIniKey>();
      ini.GetKeys(SECTION, keys);
        this._pos = keys.Where(k => !k.Name.StartsWith("$")).ToDictionary(k => k.Name, k => new ArmPos(ini.Get(k).ToString()));
        this._pos["$top"] = new ArmPos(this._rotors[0].Max);
        this._pos["$mid"] = new ArmPos(0.2f);
        this._pos["$bottom"] = new ArmPos(this._rotors[0].Min);
        this._pos["$auto-high"] = new ArmPos(ArmPos.R_ELEVATION, 0);
        this._pos["$auto-low"] = new ArmPos(ArmPos.L_ELEVATION, 0);
      var drills = new List<IMyShipDrill>();
      p.GridTerminalSystem.GetBlocksOfType(drills, d => d.IsSameConstructAs(cont));
        this._autoCont = new ArmAutoControl(ini, this.Angle, wCont, drills);

      cmd.AddCmd(new Cmd("arm-del", "Deletes a saved position of the arm", s => _deletePosition(s[0]), nArgs: 1));
      cmd.AddCmd(new Cmd("arm-elevation", "Make the arm elevate at the correct position", (s, c) => StartJob(_autoElevate, s, c), maxArgs: 2));
      cmd.AddCmd(new Cmd("arm-drill", "Engages the drills and move slowly to position", (s, c) => StartJob(_drill, s[0], c), nArgs: 1));
      cmd.AddCmd(new Cmd("arm-recall", "Recalls a saved position of the arm", (s, c) => StartJob(_recallPosition, s[0], c), nArgs: 1));
      cmd.AddCmd(new Cmd("arm-save", "Saves the position of the arm", s => _savePosition(s[0]), nArgs: 1));
      Schedule(new ScheduledAction(() => this._updateRotors(cont), name: "arm-handle"));
      ScheduleOnSave(_save);
    }

    void _save(MyIni ini) {
      foreach(KeyValuePair<string, ArmPos> kv in this._pos.Where(k => !k.Key.StartsWith("$")))
        ini.Set(SECTION, kv.Key, kv.Value.ToString());
        this._autoCont.Save(ini);
    }

    void _savePosition(string name) => this._pos[name] = this._autoCont.Target;

    void _recallPosition(string name) {
      if (this._pos.ContainsKey(name))
          this._autoCont.SetTarget(this._pos[name]);
    }

    void _drill(string name) {
      if (this._pos.ContainsKey(name)) {
          this._autoCont.SetTarget(this._pos[name]);
          this._autoCont.SwitchDrills(true);
      }
    }

    void _autoElevate(List<string> s) => this._autoCont.SetTarget(new ArmPos(
        s.Count == 0 ? ArmPos.L_ELEVATION : s[0] == "high" ? ArmPos.R_ELEVATION : s[0] == "low" ? ArmPos.R_ELEVATION : double.Parse(s[0]),
        s.Count > 1 ? MathHelper.ToRadians(float.Parse(s[1])) : 0));

    void _deletePosition(string name) => this._pos.Remove(name);

    void _updateRotors(IMyShipController cont) {
      float speed = -Math.Sign(cont.RollIndicator);
      if(speed != 0f) {
          this._rotors.ForEach(r => r.Move(speed));
          this._autoCont.SetTarget(new ArmPos(this.Angle));
      } else if (this._autoCont.Control(this._rotors, cont))
        StopCallback("Arm position reached");
    }
  }
}
}
