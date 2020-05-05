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
    public class ArmRotor {
      public float Angle => this.rev ? -this.rot.Angle : this.rot.Angle;
      public float Max => this.rev ? -this.rot.LowerLimitRad : this.rot.UpperLimitRad;
      public float Min => this.rev ? -this.rot.UpperLimitRad : this.rot.LowerLimitRad;
      public float AngleProxy(float angle) => this.rev ? - this.rot.AngleProxy(-angle) : this.rot.AngleProxy(angle);

      readonly IMyMotorStator rot;
      readonly bool rev;

      public ArmRotor(IMyMotorStator rotor, bool reversed) {
        this.rot = rotor;
        this.rev = reversed;
      }

      public void Move(float speed) {
        this.rot.TargetVelocityRad = speed * (this.rev ? -0.1f : 0.1f);
        this.rot.Enabled = true;
      }
    }
  }
}
