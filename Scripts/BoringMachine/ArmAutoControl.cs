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
    public enum ArmPosType { Angle = 0, Elevation, None }

    public struct ArmPos {
      public static readonly ArmPos NONE = new ArmPos("");
      public const double L_ELEVATION = 2;
      public const double R_ELEVATION = 4.50;

      public readonly ArmPosType Type;
      public readonly float Angle;
      public readonly double Elevation;
      public bool IsNone => Type == ArmPosType.None;
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
        Elevation = 0;
        if (s == "") {
          Type = ArmPosType.None;
          Angle = 0;
        } else {
          string[] ss = s.Split(IniHelper.SEP);
          Angle = float.Parse(ss[0]);
          if (ss.Count() == 2) {
            Type = ArmPosType.Elevation;
            Elevation = double.Parse(ss[1]);
          } else {
            Type = ArmPosType.Angle;
          }
        }
      }
      public override string ToString() => IsNone ? "" : (Type == ArmPosType.Angle ? $"{Angle}" : $"{Angle},{Elevation}");
    }

    public class ArmAutoControl {
      static readonly string SECTION = "arm-status";

      public ArmPos CurrentTarget { get; set; } = ArmPos.NONE;

      ArmPos _next = ArmPos.NONE;
      readonly List<IMyShipDrill> _drills;
      readonly WheelsController _wc;

      bool IsFlailing => !_next.IsNone;

      public ArmAutoControl(MyIni ini, float angle, WheelsController wc, List<IMyShipDrill> drills) {
        if (ini.ContainsSection(SECTION)) {
          CurrentTarget = new ArmPos(ini.Get(SECTION, "pos1").ToString());
          _next = new ArmPos(ini.Get(SECTION, "pos2").ToString());
        } else {
          SetTarget(new ArmPos(angle));
        }
        _wc = wc;
        _drills = drills;
      }

      public void SetTarget(ArmPos pos1, ArmPos? pos2 = null) {
        if (CurrentTarget.Type == ArmPosType.Elevation) {
          _wc.SetRoll(0);
        }
        CurrentTarget = pos1;
        _next = pos2 ?? ArmPos.NONE;
      }

      public bool Control(List<ArmRotor> rotors, IMyShipController cont) {
        bool anyMoving = false;
        if(CurrentTarget.Type == ArmPosType.Angle) {
          foreach(var rotor in rotors) {
            float delta = AngleProxy(rotor.Angle, CurrentTarget.Angle);
            float maxSpeed = IsFlailing ? 0.5f : 4;
            rotor.Move(MathHelper.Clamp(delta * 30, -maxSpeed, maxSpeed));
            anyMoving |= Math.Abs(delta) > 0.01;
          }
        } else {
          var g = Vector3D.Normalize(cont.GetNaturalGravity()); // gravity direction
          var o = _orthoNorm(cont.WorldMatrix.Forward, g); // orthogonal to gravity, in the same plane than g and forward direction
          var r = g.Cross(o); // orthogonal to g and o, dimension to be ignored

          var d = Math.Cos(CurrentTarget.Angle) * g + Math.Sin(CurrentTarget.Angle) * o; // normal to desired slope plane
          var wPlane = _wc.GetContactPlaneW(); // normal of the contact plane

          var wPitch = _orthoNorm(wPlane, r);
          var fp = _orthoNorm(cont.WorldMatrix.Forward, wPitch);
          var wRoll = _orthoNorm(wPlane, fp);

          double curPitch = Math.Asin(wPitch.Dot(o));
          double curRoll = Math.Asin(wRoll.Dot(r));
          _wc.SetRoll((float)curRoll);
          var poc = _wc.GetPointOfContactW(fp);
          var drillPos = _getDrillsPosW();
          double curElevation = (poc - drillPos).Dot(d); // elevation of drills above the plane being drilled
          double delta = CurrentTarget.Elevation - curElevation;
          double deltaAngle = CurrentTarget.Angle - curPitch;

          // TODO fine tune deltaAngle
          float speed = MathHelper.Clamp((float)(delta + deltaAngle) * 20, -4, 4);

          foreach(var rotor in rotors) {
            rotor.Move(speed);
            anyMoving |= Math.Abs(delta) > 0.01;
          }
        }
        if (!anyMoving && IsFlailing) {
          var temp = CurrentTarget;
          CurrentTarget = _next;
          _next = temp;
          return true;
        }
        return anyMoving;
      }

      public void Save(MyIni ini) {
        ini.Set(SECTION, "pos1", CurrentTarget.ToString());
        if (!_next.IsNone) {
          ini.Set(SECTION, "pos2", _next.ToString());
        }
      }

      public static float AngleProxy(float A1, float A2) {
        A1 = A2 - A1;
        A1 = (float)Mod((double)A1 + Math.PI, 2 * Math.PI) - (float)Math.PI;
        return A1;
      }

      public static double Mod(double A, double N) => A - (Math.Floor(A / N) * N);

      Vector3D _getDrillsPosW() {
        var pos = new Vector3D(0, 0, 0);
        foreach(var d in _drills) {
          pos += d.GetPosition() + (0.75 * d.WorldMatrix.Forward);
        }
        return pos / _drills.Count();
      }

      Vector3D _orthoNorm(Vector3D v, Vector3D refV) => Vector3D.Normalize(v - (v.Dot(refV) * refV));
    }
  }
}
