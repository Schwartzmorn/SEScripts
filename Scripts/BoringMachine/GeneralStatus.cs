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
      readonly List<IMyReflectorLight> armLights = new List<IMyReflectorLight>();
      readonly List<IMyReflectorLight> frontLights = new List<IMyReflectorLight>();
      readonly ArmController arm;
      readonly ConnectionClient conClient;

      public float ArmAngle => this.arm.Angle;
      public float ArmTarget => this.arm.TargetAngle;

      public ConnectionState ConnectionState => this.conClient.State;
      public FailReason FailReason => this.conClient.FailReason;
      public float Progress => this.conClient.Progress;

      public GeneralStatus(MyGridProgram program, ArmController arm, ConnectionClient conClient) {
        IMyGridTerminalSystem gts = program.GridTerminalSystem;
        IMyCubeGrid grid = program.Me.CubeGrid;
        gts.GetBlocksOfType(this.armLights, l => grid != l.CubeGrid && l.CubeGrid.IsSameConstructAs(grid));
        gts.GetBlocksOfType(this.frontLights, l => l.CubeGrid == grid && l.DisplayNameText.Contains("Front"));
        this.arm = arm;
        this.conClient = conClient;
      }

      public bool AreArmLightsOn => this.armLights.Any(l => l.Enabled);

      public bool AreFrontLightsOn => this.frontLights.Any(l => l.Enabled);
    }
  }
}
