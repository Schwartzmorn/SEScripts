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
    public class GeneralStatus {
      readonly List<IMyReflectorLight> _armLights = new List<IMyReflectorLight>();
      readonly List<IMyReflectorLight> _frontLights = new List<IMyReflectorLight>();

      public float ArmAngle { get; private set; } = 0;
      public float ArmTarget { get; private set; } = 0;

      public ConnectionState ConnectionState { get; private set; }
      public FailReason FailReason { get; private set; }
      public float Progress { get; private set; } = 0;

      public GeneralStatus(MyGridProgram program, CommandLine cmd) {
        IMyGridTerminalSystem gts = program.GridTerminalSystem;
        IMyCubeGrid grid = program.Me.CubeGrid;
        gts.GetBlocksOfType(this._armLights, l => grid != l.CubeGrid && l.CubeGrid.IsSameConstructAs(grid));
        gts.GetBlocksOfType(this._frontLights, l => l.CubeGrid == grid && l.CustomName.Contains("Front"));
        cmd.RegisterCommand(new Command("gs-arm", Command.Wrap(this.handleArm), "",  nArgs: 2));
        cmd.RegisterCommand(new Command("gs-con", Command.Wrap(this.handleConnnection), "", nArgs: 3));
      }

      public bool AreArmLightsOn => this._armLights.Any(l => l.Enabled);

      public bool AreFrontLightsOn => this._frontLights.Any(l => l.Enabled);

      void handleArm(List<string> args) {
        this.ArmAngle = float.Parse(args[0]);
        this.ArmTarget = float.Parse(args[1]);
      }

      void handleConnnection(List<string> args) {
        this.ConnectionState = (ConnectionState)Enum.Parse(this.ConnectionState.GetType(), args[0]);
        this.FailReason = (FailReason)Enum.Parse(this.FailReason.GetType(), args[1]);
        this.Progress = float.Parse(args[2]);
      }
    }
  }
}
