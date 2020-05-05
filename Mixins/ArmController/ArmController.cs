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
      readonly List<ArmRotor> rotors;
      readonly Dictionary<string, ArmPos> pos;
      readonly ArmAutoControl autoCont;
      Process currentProcess;

      public float Angle => this.rotors[0].Angle;

      // TODO correct this
      public float TargetAngle => this.autoCont.Target.Angle;

      public ArmController(MyIni ini, MyGridProgram p, CommandLine cmd, IMyShipController cont, WheelsController wCont, ISaveManager manager) {
        var rotors = new List<IMyMotorStator>();
        p.GridTerminalSystem.GetBlocksOfType(rotors, r => r.CubeGrid == p.Me.CubeGrid && r.DisplayNameText.Contains("Arm Rotor"));
        this.rotors = rotors.Select(r => new ArmRotor(r, r.WorldMatrix.Up.Dot(cont.WorldMatrix.Right) > 0)).ToList();
        var keys = new List<MyIniKey>();
        ini.GetKeys(SECTION, keys);
        this.pos = keys.Where(k => !k.Name.StartsWith("$")).ToDictionary(k => k.Name, k => new ArmPos(ini.Get(k).ToString()));
        this.pos["$top"] = new ArmPos(this.rotors[0].Max);
        this.pos["$mid"] = new ArmPos(0.2f);
        this.pos["$bottom"] = new ArmPos(this.rotors[0].Min);
        this.pos["$auto-high"] = new ArmPos(ArmPos.R_ELEVATION, 0);
        this.pos["$auto-low"] = new ArmPos(ArmPos.L_ELEVATION, 0);
        var tools = new List<IMyFunctionalBlock>();
        p.GridTerminalSystem.GetBlocksOfType(tools, t => t.IsSameConstructAs(cont) && (t is IMyShipToolBase || t is IMyShipDrill));
        this.autoCont = new ArmAutoControl(ini, this.Angle, wCont, tools);

        cmd.RegisterCommand(new Command("arm-del", Command.Wrap(this.deletePosition), "Deletes a saved position of the arm", nArgs: 1));
        cmd.RegisterCommand(new Command("arm-elevation", Command.Wrap(this.autoElevate), "Makes the arm elevate at the correct position", detailedHelp: @"First argument is elevation ('high'/'low'/float)
Second argument is angle", maxArgs: 2));
        cmd.RegisterCommand(new Command("arm-drill", Command.Wrap(this.drill), "Engages the drills and move slowly to position", nArgs: 1));
        cmd.RegisterCommand(new Command("arm-recall", Command.Wrap(this.recallPosition), "Recalls a saved position of the arm", nArgs: 1));
        cmd.RegisterCommand(new Command("arm-save", Command.Wrap(this.savePosition), "Saves the current position of the arm", nArgs: 1));
        manager.Spawn(pc => this.updateRotors(cont), "arm-handle");
        manager.AddOnSave(this.save);
      }

      void save(MyIni ini) {
        foreach (KeyValuePair<string, ArmPos> kv in this.pos.Where(k => !k.Key.StartsWith("$"))) {
          ini.Set(SECTION, kv.Key, kv.Value.ToString());
        }
        this.autoCont.Save(ini);
      }

      void savePosition(string name) => this.pos[name] = this.autoCont.Target;

      void recallPosition(Process p, string name) {
        this.checkProcess(p);
        if (this.pos.ContainsKey(name)) {
          this.autoCont.SetTarget(this.pos[name]);
        }
      }

      void drill(Process p, string name) {
        this.checkProcess(p);
        if (this.pos.ContainsKey(name)) {
          this.autoCont.SetTarget(this.pos[name]);
          this.autoCont.SwitchTools(true);
        }
      }

      void autoElevate(Process p, List<string> s) {
        this.checkProcess(p);
        this.autoCont.SetTarget(new ArmPos(
          s.Count == 0 ? ArmPos.L_ELEVATION : s[0] == "high" ? ArmPos.R_ELEVATION : s[0] == "low" ? ArmPos.R_ELEVATION : double.Parse(s[0]),
          s.Count > 1 ? MathHelper.ToRadians(float.Parse(s[1])) : 0));
      }

      void checkProcess(Process currentProcess) {
        if (!ReferenceEquals(this.currentProcess, currentProcess)) {
          this.currentProcess?.Kill();
          this.currentProcess = currentProcess;
        }
      }

      void endProcess() {
        this.currentProcess?.Done();
        this.currentProcess = null;
      }

      void deletePosition(string name) => this.pos.Remove(name);

      void updateRotors(IMyShipController cont) {
        float speed = -Math.Sign(cont.RollIndicator);
        if (speed != 0f) {
          this.rotors.ForEach(r => r.Move(speed));
          this.autoCont.SetTarget(new ArmPos(this.Angle));
          this.endProcess();
        } else if (this.autoCont.Control(this.rotors, cont)) {
          this.endProcess();
        }
      }
    }
  }
}
