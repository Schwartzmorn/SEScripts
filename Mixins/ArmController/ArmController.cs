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
      const string SECTION = "arm-positions";
      readonly List<ArmRotor> _rotors;
      readonly Dictionary<string, ArmPos> _pos;
      readonly ArmAutoControl _autoCont;
      Process _currentProcess;

      public float Angle => _rotors[0].Angle;

      // TODO correct this
      public float TargetAngle => _autoCont.Target.Angle;

      public ArmController(MyIni ini, MyGridProgram p, CommandLine cmd, IMyShipController cont, WheelsController wCont, ISaveManager manager) {
        var rotors = new List<IMyMotorStator>();
        p.GridTerminalSystem.GetBlocksOfType(rotors, r => r.CubeGrid == p.Me.CubeGrid && r.CustomName.Contains("Arm Rotor"));
        _rotors = rotors.Select(r => new ArmRotor(r, r.WorldMatrix.Up.Dot(cont.WorldMatrix.Right) > 0)).ToList();
        var keys = new List<MyIniKey>();
        ini.GetKeys(SECTION, keys);
        _pos = keys.Where(k => !k.Name.StartsWith("$")).ToDictionary(k => k.Name, k => new ArmPos(ini.Get(k).ToString()));
        _pos["$top"] = new ArmPos(_rotors[0].Max);
        _pos["$mid"] = new ArmPos(0.2f);
        _pos["$bottom"] = new ArmPos(_rotors[0].Min);
        _pos["$auto-high"] = new ArmPos(ArmPos.R_ELEVATION, 0);
        _pos["$auto-low"] = new ArmPos(ArmPos.L_ELEVATION, 0);
        var tools = new List<IMyFunctionalBlock>();
        p.GridTerminalSystem.GetBlocksOfType(tools, t => t.IsSameConstructAs(cont) && (t is IMyShipToolBase || t is IMyShipDrill));
        _autoCont = new ArmAutoControl(ini, Angle, wCont, tools);

        cmd.RegisterCommand(new Command("arm-del", Command.Wrap(_deletePosition), "Deletes a saved position of the arm", nArgs: 1));
        cmd.RegisterCommand(new Command("arm-elevation", Command.Wrap(_autoElevate), "Makes the arm elevate at the correct position", detailedHelp: @"First argument is elevation ('high'/'low'/float)
Second argument is angle", maxArgs: 2));
        cmd.RegisterCommand(new Command("arm-drill", Command.Wrap(_drill), "Engages the drills and move slowly to position", nArgs: 1));
        cmd.RegisterCommand(new Command("arm-recall", Command.Wrap(_recallPosition), "Recalls a saved position of the arm", nArgs: 1));
        cmd.RegisterCommand(new Command("arm-save", Command.Wrap(_savePosition), "Saves the current position of the arm", nArgs: 1));
        manager.Spawn(pc => _updateRotors(cont), "arm-handle");
        manager.AddOnSave(_save);
      }

      void _save(MyIni ini) {
        foreach (KeyValuePair<string, ArmPos> kv in _pos.Where(k => !k.Key.StartsWith("$"))) {
          ini.Set(SECTION, kv.Key, kv.Value.ToString());
        }
        _autoCont.Save(ini);
      }

      void _savePosition(string name) => _pos[name] = _autoCont.Target;

      void _recallPosition(Process p, string name) {
        _checkProcess(p);
        if (_pos.ContainsKey(name)) {
          _autoCont.SetTarget(_pos[name]);
        }
      }

      void _drill(Process p, string name) {
        _checkProcess(p);
        if (_pos.ContainsKey(name)) {
          _autoCont.SetTarget(_pos[name]);
          _autoCont.SwitchTools(true);
        }
      }

      void _autoElevate(Process p, List<string> s) {
        _checkProcess(p);
        _autoCont.SetTarget(new ArmPos(
          s.Count == 0 ? ArmPos.L_ELEVATION : s[0] == "high" ? ArmPos.R_ELEVATION : s[0] == "low" ? ArmPos.R_ELEVATION : double.Parse(s[0]),
          s.Count > 1 ? MathHelper.ToRadians(float.Parse(s[1])) : 0));
      }

      void _checkProcess(Process currentProcess) {
        if (!ReferenceEquals(_currentProcess, currentProcess)) {
          _currentProcess?.Kill();
          _currentProcess = currentProcess;
        }
      }

      void _endProcess() {
        _currentProcess?.Done();
        _currentProcess = null;
      }

      void _deletePosition(string name) => _pos.Remove(name);

      void _updateRotors(IMyShipController cont) {
        float speed = -Math.Sign(cont.RollIndicator);
        if (speed != 0f) {
          _rotors.ForEach(r => r.Move(speed));
          _autoCont.SetTarget(new ArmPos(Angle));
          _endProcess();
        } else if (_autoCont.Control(_rotors, cont)) {
          _endProcess();
        }
      }
    }
  }
}
