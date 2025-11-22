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
      string[] ss = s.Split(IniHelper.SEP);
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

    readonly List<IMyFunctionalBlock> _tools;
    readonly WheelsController _wc;

    public ArmAutoControl(MyIni ini, float angle, WheelsController wc, List<IMyFunctionalBlock> tools) {
      _tools = tools;
      _wc = wc;
      if (ini.ContainsSection(SECTION))
          Target = new ArmPos(ini.Get(SECTION, "pos").ToString());
      else
          SetTarget(new ArmPos(angle));
    }

    bool IsDrilling => _tools.Any(d => d.Enabled);

    public void SetTarget(ArmPos pos) {
        SwitchTools(false);
        Target = pos;
    }

    public bool Control(List<ArmRotor> rotors, IMyShipController cont) {
      float maxSpeed = IsDrilling ? 0.5f : 4;
      bool reachedDestination = true;
      if(Target.Type == ArmPosType.Angle) {
        foreach (ArmRotor r in rotors) {
          float delta = r.AngleProxy(Target.Angle);
          r.Move(MathHelper.Clamp(delta * 30, -maxSpeed, maxSpeed));
          reachedDestination &= Math.Abs(delta) < 0.01;
        }
      } else {
        var g = Vector3D.Normalize(cont.GetNaturalGravity()); // gravity direction
        Vector3D o = _orthoNorm(cont.WorldMatrix.Forward, g); // orthogonal to gravity, in the same plane than g and forward direction
        Vector3D p = g.Cross(o); // orthogonal to g and o, dimension to be ignored

        Vector3D d = Math.Cos(Target.Angle) * g + Math.Sin(Target.Angle) * o; // normal to desired slope plane
        Vector3D wPlane = _wc.GetContactPlaneW(); // normal of the contact plane

        Vector3D wPitch = _orthoNorm(wPlane, p); // yaw axis
        Vector3D fp = _orthoNorm(cont.WorldMatrix.Forward, wPitch); // forward direction somewhat in the plane of contact
        Vector3D wRoll = _orthoNorm(wPlane, fp); // pitch axis

        double curPitch = Math.Asin(wPitch.Dot(o));
        Vector3D poc = _wc.GetPointOfContactW(fp);
        Vector3D drillPos = _getToolsPosW();
        double curElevation = (poc - drillPos).Dot(d); // elevation of drills above the plane being drilled
        double delta = Target.Elevation - curElevation;
        double deltaAngle = Target.Angle - curPitch;

        // Can always go up as fast as possible
        float speed = MathHelper.Clamp((float)(delta + deltaAngle) * 20, -maxSpeed, 4);

        rotors.ForEach(r => r.Move(speed));
        reachedDestination = Math.Abs(delta) < 0.01;
      }
      return reachedDestination;
    }

    public void Save(MyIni ini) => ini.Set(SECTION, "pos", Target.ToString());

    public void SwitchTools(bool s) => _tools.ForEach(d =>
    {
      if (d.Enabled != s)
      {
        d.Enabled = s;
      }
    });

    Vector3D _getToolsPosW() {
      var pos = new Vector3D(0, 0, 0);
      _tools.ForEach(t => pos += t.GetPosition() + (0.75 * t.WorldMatrix.Forward));
      return pos / _tools.Count();
    }

    Vector3D _orthoNorm(Vector3D v, Vector3D refV) => Vector3D.Normalize(v - (v.Dot(refV) * refV));
  }
}
}
