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
  public enum ArmPosType { Angle = 0, Elevation }

  public struct ArmPos {
    public const double L_ELEVATION = 2;
    public const double R_ELEVATION = 4.50;

    public readonly ArmPosType Type;
    public readonly float Angle;
    public readonly double Elevation;
    public ArmPos(float angle) {
      Type = ArmPosType.Angle;
      Angle = angle;
      Elevation = 0;
    }
    public ArmPos(double elevation, float angle) {
      Type = ArmPosType.Elevation;
      Angle = angle;
      Elevation = elevation;
    }
    public ArmPos(string s) {
      string[] ss = s.Split(Ini.SEP);
      Angle = float.Parse(ss[0]);
      if (ss.Count() == 2) {
        Type = ArmPosType.Elevation;
        Elevation = double.Parse(ss[1]);
      } else {
        Type = ArmPosType.Angle;
        Elevation = 0;
      }
    }
    public override string ToString() => Type == ArmPosType.Angle ? $"{Angle}" : $"{Angle},{Elevation}";
  }

  public class ArmAutoControl {
    static readonly string SECTION = "arm-status";

    public ArmPos Target { get; private set; }

    readonly List<IMyShipDrill> _drills;
    readonly WheelsController _wc;

    public ArmAutoControl(Ini ini, float angle, WheelsController wc, List<IMyShipDrill> drills) {
      if (ini.ContainsSection(SECTION))
        Target = new ArmPos(ini.Get(SECTION, "pos").ToString());
      else
        SetTarget(new ArmPos(angle));
      _wc = wc;
      _drills = drills;
    }

    bool IsDrilling => _drills.Any(d => d.Enabled);

    public void SetTarget(ArmPos pos) {
      SwitchDrills(false);
      if (Target.Type == ArmPosType.Angle)
        _wc.SetRoll(0);
      Target = pos;
    }

    public bool Control(List<ArmRotor> rotors, IMyShipController cont) {
      float maxSpeed = IsDrilling ? 0.2f : 4;
      bool moving = false;
      if(Target.Type == ArmPosType.Angle) {
        foreach(var r in rotors) {
          float delta = AngleProxy(r.Angle, Target.Angle);
          r.Move(MathHelper.Clamp(delta * 30, -maxSpeed, maxSpeed));
          moving |= Math.Abs(delta) > 0.01;
        }
      } else {
        var g = Vector3D.Normalize(cont.GetNaturalGravity()); // gravity direction
        var o = _orthoNorm(cont.WorldMatrix.Forward, g); // orthogonal to gravity, in the same plane than g and forward direction
        var p = g.Cross(o); // orthogonal to g and o, dimension to be ignored

        var d = Math.Cos(Target.Angle) * g + Math.Sin(Target.Angle) * o; // normal to desired slope plane
        var wPlane = _wc.GetContactPlaneW(); // normal of the contact plane

        var wPitch = _orthoNorm(wPlane, p);
        var fp = _orthoNorm(cont.WorldMatrix.Forward, wPitch);
        var wRoll = _orthoNorm(wPlane, fp);

        double curPitch = Math.Asin(wPitch.Dot(o));
        double curRoll = Math.Asin(wRoll.Dot(p));
        _wc.SetRoll((float)curRoll);
        var poc = _wc.GetPointOfContactW(fp);
        var drillPos = _getDrillsPosW();
        double curElevation = (poc - drillPos).Dot(d); // elevation of drills above the plane being drilled
        double delta = Target.Elevation - curElevation;
        double deltaAngle = Target.Angle - curPitch;

        // Can always go up as fast as possible
        float speed = MathHelper.Clamp((float)(delta + deltaAngle) * 20, -maxSpeed, 4);

        rotors.ForEach(r => r.Move(speed));
        moving = Math.Abs(delta) > 0.01;
      }
      return !moving;
    }

    public void Save(MyIni ini) => ini.Set(SECTION, "pos", Target.ToString());

    public void SwitchDrills(bool s) => _drills.ForEach(d => d.Enabled = s);

    public static float AngleProxy(float A1, float A2) {
      A1 = A2 - A1;
      A1 = (float)Mod(A1 + Math.PI, 2 * Math.PI) - MathHelper.Pi;
      return A1;
    }

    public static double Mod(double A, double N) => A - (Math.Floor(A / N) * N);

    Vector3D _getDrillsPosW() {
      var pos = new Vector3D(0, 0, 0);
      _drills.ForEach(d => pos += d.GetPosition() + (0.75 * d.WorldMatrix.Forward));
      return pos / _drills.Count();
    }

    Vector3D _orthoNorm(Vector3D v, Vector3D refV) => Vector3D.Normalize(v - (v.Dot(refV) * refV));
  }
}
}
