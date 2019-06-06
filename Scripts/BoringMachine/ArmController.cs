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

    public float Angle =>  _rotors[0].Angle;

    // TODO correct this
    public float TargetAngle => _autoCont.Target.Angle;

    public ArmController(Ini ini, MyGridProgram p, CmdLine cmd, IMyShipController cont, WheelsController wCont) {
      var rotors = new List<IMyMotorStator>();
      p.GridTerminalSystem.GetBlocksOfType(rotors, r => r.CubeGrid == p.Me.CubeGrid && r.DisplayNameText.Contains("Arm Rotor"));
      _rotors = rotors.Select(r => new ArmRotor(r, r.WorldMatrix.Up.Dot(cont.WorldMatrix.Right) > 0)).ToList();
      var keys = new List<MyIniKey>();
      ini.GetKeys(SECTION, keys);
      _pos = keys.Where(k => !k.Name.StartsWith("$")).ToDictionary(k => k.Name, k => new ArmPos(ini.Get(k).ToString()));
      _pos["$top"] = new ArmPos(_rotors[0].Max);
      _pos["$mid"] = new ArmPos(0.2f);
      _pos["$bottom"] = new ArmPos(_rotors[0].Min);
      _pos["$auto-high"] = new ArmPos(ArmPos.R_ELEVATION, 0);
      _pos["$auto-low"] = new ArmPos(ArmPos.L_ELEVATION, 0);
      var drills = new List<IMyShipDrill>();
      p.GridTerminalSystem.GetBlocksOfType(drills, d => d.IsSameConstructAs(cont));
      _autoCont = new ArmAutoControl(ini, Angle, wCont, drills);

      cmd.AddCmd(new Cmd("arm-del", "Deletes a saved position of the arm", s => _deletePosition(s[0]), nArgs: 1));
      cmd.AddCmd(new Cmd("arm-elevation", "Make the arm elevate at the correct position", (s, c) => StartJob(_autoElevate, s, c), maxArgs: 2));
      cmd.AddCmd(new Cmd("arm-drill", "Engages the drills and move slowly to position", (s, c) => StartJob(_drill, s[0], c), nArgs: 1));
      cmd.AddCmd(new Cmd("arm-recall", "Recalls a saved position of the arm", (s, c) => StartJob(_recallPosition, s[0], c), nArgs: 1));
      cmd.AddCmd(new Cmd("arm-save", "Saves the position of the arm", s => _savePosition(s[0]), nArgs: 1));
      Schedule(new ScheduledAction(() => _updateRotors(cont), name: "arm-handle"));
      ScheduleOnSave(_save);
    }

    void _save(MyIni ini) {
      foreach(var kv in _pos.Where(k => !k.Key.StartsWith("$")))
        ini.Set(SECTION, kv.Key, kv.Value.ToString());
      _autoCont.Save(ini);
    }

    void _savePosition(string name) => _pos[name] = _autoCont.Target;

    void _recallPosition(string name) {
      if (_pos.ContainsKey(name))
        _autoCont.SetTarget(_pos[name]);
    }

    void _drill(string name) {
      if (_pos.ContainsKey(name)) {
        _autoCont.SetTarget(_pos[name]);
        _autoCont.SwitchDrills(true);
      }
    }

    void _autoElevate(List<string> s) => _autoCont.SetTarget(new ArmPos(
        s.Count == 0 ? ArmPos.L_ELEVATION : s[0] == "high" ? ArmPos.R_ELEVATION : s[0] == "low" ? ArmPos.R_ELEVATION : double.Parse(s[0]),
        s.Count > 1 ? MathHelper.ToRadians(float.Parse(s[1])) : 0));

    void _deletePosition(string name) => _pos.Remove(name);

    void _updateRotors(IMyShipController cont) {
      float speed = -Math.Sign(cont.RollIndicator);
      if(speed != 0f) {
        _rotors.ForEach(r => r.Move(speed));
        _autoCont.SetTarget(new ArmPos(Angle));
      } else if (_autoCont.Control(_rotors, cont))
        StopCallback("Arm position reached");
    }
  }
}
}
