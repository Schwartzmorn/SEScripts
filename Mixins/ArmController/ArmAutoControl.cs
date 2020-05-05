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
        this.Type = ArmPosType.Angle;
        this.Angle = angle;
        this.Elevation = 0;
    }
    public ArmPos(double elevation, float angle) {
        this.Type = ArmPosType.Elevation;
        this.Angle = angle;
        this.Elevation = elevation;
    }
    public ArmPos(string s) {
      string[] ss = s.Split(IniHelper.SEP);
      this.Angle = float.Parse(ss[0]);
      if (ss.Count() == 2) {
          this.Type = ArmPosType.Elevation;
          this.Elevation = double.Parse(ss[1]);
      } else {
          this.Type = ArmPosType.Angle;
          this.Elevation = 0;
      }
    }
    public override string ToString() => this.Type == ArmPosType.Angle ? $"{this.Angle}" : $"{this.Angle},{this.Elevation}";
  }

  public class ArmAutoControl {
    static readonly string SECTION = "arm-status";

    public ArmPos Target { get; private set; }

    readonly List<IMyFunctionalBlock> tools;
    readonly WheelsController wc;

    public ArmAutoControl(MyIni ini, float angle, WheelsController wc, List<IMyFunctionalBlock> tools) {
      this.tools = tools;
      this.wc = wc;
      if (ini.ContainsSection(SECTION))
          this.Target = new ArmPos(ini.Get(SECTION, "pos").ToString());
      else
          this.SetTarget(new ArmPos(angle));
    }

    bool IsDrilling => this.tools.Any(d => d.Enabled);

    public void SetTarget(ArmPos pos) {
        this.SwitchTools(false);
        this.Target = pos;
    }

    public bool Control(List<ArmRotor> rotors, IMyShipController cont) {
      float maxSpeed = this.IsDrilling ? 0.5f : 4;
      bool moving = false;
      if(this.Target.Type == ArmPosType.Angle) {
        foreach (ArmRotor r in rotors) {
          float delta = r.AngleProxy(this.Target.Angle);
          r.Move(MathHelper.Clamp(delta * 30, -maxSpeed, maxSpeed));
          moving |= Math.Abs(delta) > 0.01;
        }
      } else {
        var g = Vector3D.Normalize(cont.GetNaturalGravity()); // gravity direction
        Vector3D o = this.orthoNorm(cont.WorldMatrix.Forward, g); // orthogonal to gravity, in the same plane than g and forward direction
        Vector3D p = g.Cross(o); // orthogonal to g and o, dimension to be ignored

        Vector3D d = Math.Cos(this.Target.Angle) * g + Math.Sin(this.Target.Angle) * o; // normal to desired slope plane
        Vector3D wPlane = this.wc.GetContactPlaneW(); // normal of the contact plane

        Vector3D wPitch = this.orthoNorm(wPlane, p); // yaw axis
        Vector3D fp = this.orthoNorm(cont.WorldMatrix.Forward, wPitch); // forward direction somewhat in the plane of contact
        Vector3D wRoll = this.orthoNorm(wPlane, fp); // pitch axis

        double curPitch = Math.Asin(wPitch.Dot(o));
        Vector3D poc = this.wc.GetPointOfContactW(fp);
        Vector3D drillPos = this.getToolsPosW();
        double curElevation = (poc - drillPos).Dot(d); // elevation of drills above the plane being drilled
        double delta = this.Target.Elevation - curElevation;
        double deltaAngle = this.Target.Angle - curPitch;

        // Can always go up as fast as possible
        float speed = MathHelper.Clamp((float)(delta + deltaAngle) * 20, -maxSpeed, 4);

        rotors.ForEach(r => r.Move(speed));
        moving = Math.Abs(delta) > 0.01;
      }
      return !moving;
    }

    public void Save(MyIni ini) => ini.Set(SECTION, "pos", this.Target.ToString());

    public void SwitchTools(bool s) => this.tools.ForEach(d => d.Enabled = s);

    Vector3D getToolsPosW() {
      var pos = new Vector3D(0, 0, 0);
      this.tools.ForEach(t => pos += t.GetPosition() + (0.75 * t.WorldMatrix.Forward));
      return pos / this.tools.Count();
    }

    Vector3D orthoNorm(Vector3D v, Vector3D refV) => Vector3D.Normalize(v - (v.Dot(refV) * refV));
  }
}
}
