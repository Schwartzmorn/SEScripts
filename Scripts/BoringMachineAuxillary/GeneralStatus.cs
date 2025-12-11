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

namespace IngameScript
{
  partial class Program
  {
    public class GeneralStatus
    {
      readonly List<IMyReflectorLight> _armLights = new List<IMyReflectorLight>();
      readonly List<IMyReflectorLight> _frontLights = new List<IMyReflectorLight>();

      public float ArmAngle { get; private set; } = 0;
      public float ArmTarget { get; private set; } = 0;

      public ConnectionState ConnectionState { get; private set; }
      public FailReason FailReason { get; private set; }
      public float Progress { get; private set; } = 0;

      public GeneralStatus(MyGridProgram program, CommandLine cmd)
      {
        IMyGridTerminalSystem gts = program.GridTerminalSystem;
        IMyCubeGrid grid = program.Me.CubeGrid;
        gts.GetBlocksOfType(_armLights, l => grid != l.CubeGrid && l.CubeGrid.IsSameConstructAs(grid));
        gts.GetBlocksOfType(_frontLights, l => l.CubeGrid == grid && l.CustomName.Contains("Front"));
        cmd.RegisterCommand(new Command("gs-arm", Command.Wrap(_handleArm), "", nArgs: 2));
        cmd.RegisterCommand(new Command("gs-con", Command.Wrap(_handleConnnection), "", nArgs: 3));
      }

      public bool AreArmLightsOn => _armLights.Any(l => l.Enabled);

      public bool AreFrontLightsOn => _frontLights.Any(l => l.Enabled);

      void _handleArm(ArgumentsWrapper args)
      {
        ArmAngle = float.Parse(args[0]);
        ArmTarget = float.Parse(args[1]);
      }

      void _handleConnnection(ArgumentsWrapper args)
      {
        ConnectionState = (ConnectionState)Enum.Parse(ConnectionState.GetType(), args[0]);
        FailReason = (FailReason)Enum.Parse(FailReason.GetType(), args[1]);
        Progress = float.Parse(args[2]);
      }
    }
  }
}
