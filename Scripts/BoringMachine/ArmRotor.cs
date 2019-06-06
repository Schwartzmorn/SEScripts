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
    public float Angle => _rev ? -_rot.Angle : _rot.Angle;
    public float Max => _rev ? -_rot.LowerLimitRad : _rot.UpperLimitRad;
    public float Min => _rev ? -_rot.UpperLimitRad : _rot.LowerLimitRad;

    readonly IMyMotorStator _rot;
    readonly bool _rev;

    public ArmRotor(IMyMotorStator rotor, bool reversed) {
      _rot = rotor;
      _rev = reversed;
    }

    public void Move(float speed) {
      _rot.TargetVelocityRad = speed * (_rev ? -0.1f : 0.1f);
      _rot.Enabled = true;
    }
  }
}
}
