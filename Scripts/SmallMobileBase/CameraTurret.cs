using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class CameraTurret {
      readonly IMyCameraBlock camera;
      readonly List<IMyShipController> controllers = new List<IMyShipController>();
      readonly IMyMotorStator pitch;
      readonly IMyMotorStator yaw;

      public CameraTurret(IMyGridTerminalSystem gts, IProcessSpawner spawner) {
        this.camera = gts.GetBlockWithName("SMB Turret Camera") as IMyCameraBlock;
        this.pitch = gts.GetBlockWithName("SMB Camera Turret Rotor") as IMyMotorStator;
        this.yaw = gts.GetBlockWithName("SMB Camera Turret Base Rotor") as IMyMotorStator;
        gts.GetBlocksOfType(this.controllers);

        spawner.Spawn(this.main, "camera-turret");
      }

      void main(Process p) {
        if (this.camera.IsActive) {
          this.pitch.TargetVelocityRad = this.controllers.Sum(c => c.IsUnderControl ? c.RotationIndicator.X : 0) / 4;
          this.yaw.TargetVelocityRad = this.controllers.Sum(c => c.IsUnderControl ? c.RotationIndicator.Y : 0) /4;
        } else {
          this.pitch.TargetVelocityRad = 0;
          this.yaw.TargetVelocityRad = 0;
        }
      }
    }
  }
}
