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
      readonly List<IMyShipController> _myShipControllers = new List<IMyShipController>();
      readonly ArmController _arm;
      readonly ConnectionClient _conClient;
      readonly WheelsController _wc;
      readonly Autopilot _ap;

      public float ArmAngle => _arm.Angle;
      public float ArmTarget => _arm.TargetAngle;

      public ConnectionState ConnectionState => _conClient.State;
      public FailReason FailReason => _conClient.FailReason;
      public float Progress => _conClient.Progress;

      public GeneralStatus(MyGridProgram program, ArmController arm, ConnectionClient conClient, WheelsController wc, Autopilot ap)
      {
        IMyGridTerminalSystem gts = program.GridTerminalSystem;
        IMyCubeGrid grid = program.Me.CubeGrid;
        gts.GetBlocksOfType(_armLights, l => grid != l.CubeGrid && l.CubeGrid.IsSameConstructAs(grid));
        gts.GetBlocksOfType(_frontLights, l => l.CubeGrid == grid && l.CustomName.Contains("Front"));
        gts.GetBlocksOfType(_myShipControllers, c => c.CubeGrid == grid);
        _arm = arm;
        _conClient = conClient;
        _wc = wc;
        _ap = ap;
      }

      public bool AreArmLightsOn => _armLights.Any(l => l.Enabled);

      public bool AreFrontLightsOn => _frontLights.Any(l => l.Enabled);

      public bool IsStrafing => _wc.IsStrafing;

      public bool IsAutopilotEngaged => _ap.Activated;

      public bool IsParkEngaged => _myShipControllers.First().HandBrake;
    }
  }
}
