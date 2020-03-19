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
    public float Angle => this._rev ? -this._rot.Angle : this._rot.Angle;
    public float Max => this._rev ? -this._rot.LowerLimitRad : this._rot.UpperLimitRad;
    public float Min => this._rev ? -this._rot.UpperLimitRad : this._rot.LowerLimitRad;

    readonly IMyMotorStator _rot;
    readonly bool _rev;

    public ArmRotor(IMyMotorStator rotor, bool reversed) {
        this._rot = rotor;
        this._rev = reversed;
    }

    public void Move(float speed) {
        this._rot.TargetVelocityRad = speed * (this._rev ? -0.1f : 0.1f);
        this._rot.Enabled = true;
    }
  }
}
}
