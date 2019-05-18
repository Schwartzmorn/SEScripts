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
    public class ArmController {
      const string SECTION = "positions";
      readonly List<ArmRotor> _rotors = new List<ArmRotor>();
      readonly Dictionary<string, ArmPos> _positions = new Dictionary<string, ArmPos>();
      readonly ArmAutoControl _autoCont;

      public float Angle => _rotors.Count > 0 ? _rotors[0].Angle : float.NaN;

      // TODO correct this
      public float TargetAngle => _autoCont.CurrentTarget.Angle;

      public ArmController(MyGridProgram p, MyIni ini, CmdLine cmd, IMyShipController cont, WheelsController wCont) {
        var rotors = new List<IMyMotorStator>();
        p.GridTerminalSystem.GetBlocksOfType(rotors, r => r.CubeGrid == p.Me.CubeGrid && r.DisplayNameText.Contains("Arm Rotor"));
        foreach (IMyMotorStator rotor in rotors) {
          // TODO check this
          _rotors.Add(new ArmRotor(rotor, rotor.WorldMatrix.Up.Dot(cont.WorldMatrix.Right) > 0));
        }
        Logger.Inst.Log($"Rotors... {_rotors.Count}/{_rotors.Count} OK");
        if (ini.ContainsSection(SECTION)) {
          var keys = new List<MyIniKey>();
          ini.GetKeys(SECTION, keys);
          foreach (MyIniKey key in keys) {
            var pos = new ArmPos(ini.Get(key).ToString());
            if (pos.Type != ArmPosType.None) {
              _positions[key.Name] = pos;
            }
          }
        }
        var drills = new List<IMyShipDrill>();
        p.GridTerminalSystem.GetBlocksOfType(drills, d => d.IsSameConstructAs(cont));
        _autoCont = new ArmAutoControl(ini, Angle, wCont, drills);

        cmd.AddCmd(new Cmd("arm-del", "Deletes a saved position of the arm", s => _deletePosition(s[0]), minArgs: 1, maxArgs: 1));
        cmd.AddCmd(new Cmd("arm-elevation", "Make the arm elevate at the correct position", s => _autoElevate(s), maxArgs: 2));
        cmd.AddCmd(new Cmd("arm-flail", "Makes the arm flail between two positions", s => _flail(s[0], s[1]), minArgs: 2, maxArgs: 2));
        cmd.AddCmd(new Cmd("arm-recall", "Recalls a saved position of the arm", s => _recallPosition(s[0]), minArgs: 1, maxArgs: 1));
        cmd.AddCmd(new Cmd("arm-save", "Saves the position of the arm", s => _savePosition(s[0]), minArgs: 1, maxArgs: 1));
        Scheduler.Inst.AddAction(() => _updateRotors(cont));
        Scheduler.Inst.AddActionOnSave(_save);
      }

      void _save(MyIni ini) {
        foreach(var kv in _positions) {
          ini.Set(SECTION, kv.Key, kv.Value.ToString());
        }
        _autoCont.Save(ini);
      }

      void _savePosition(string name) {
        if (_rotors.Count > 0) {
          _positions[name] = _autoCont.CurrentTarget;
        }
      }

      void _recallPosition(string name) {
        if (_positions.ContainsKey(name)) {
          _autoCont.SetTarget(_positions[name]);
        }
      }

      void _flail(string name1, string name2) {
        if (_positions.ContainsKey(name1) && _positions.ContainsKey(name2)) {
          _autoCont.SetTarget(
            _positions[name1],
            _positions[name2]);
        }
      }

      void _autoElevate(List<string> s) {
        if (s.Count == 0) {
          _autoCont.SetTarget(new ArmPos(ArmPos.L_ELEVATION, 0));
        } else {
          float angle = s.Count > 1 ? MathHelper.ToRadians(float.Parse(s[1])) : 0;
          double elevation = s[0] == "high" ? ArmPos.R_ELEVATION : s[0] == "low" ? ArmPos.R_ELEVATION : double.Parse(s[0]);
          _autoCont.SetTarget(new ArmPos(elevation, angle));
        }
      }

      void _deletePosition(string name) => _positions.Remove(name);

      void _updateRotors(IMyShipController cont) {
        float speed = -Math.Sign(cont.RollIndicator);
        if(cont.RollIndicator != 0f) {
          foreach(ArmRotor rotor in _rotors) {
            rotor.Move(speed);
          }
          if(_rotors.Count > 0) {
            _autoCont.SetTarget(new ArmPos(Angle));
          }
        } else {
          _autoCont.Control(_rotors, cont);
        }
      }
    }
  }
}
