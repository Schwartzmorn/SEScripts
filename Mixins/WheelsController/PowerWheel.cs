using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using VRage.Game;
using VRageMath;

namespace IngameScript {
partial class Program {
  public class PowerWheel {
    static readonly Vector3D UP = new Vector3D(0, 1, 0);
    static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);
    const string PO = "Propulsion override";
    const string SO = "Steer override";

    public float Angle => _w.SteerAngle;
    public readonly MyCubeSize CubeSize;
    public readonly float Mass;
    public readonly Vector3D Position;
    public float Power {
      get { return _revPow ? -_w.GetValueFloat(PO) : _w.GetValueFloat(PO); }
      set { _w.SetValue(PO, _revPow ? -value : value); }
    }
    public float Steer {
      get { return (_base.CenterOfTurnZ < Position.Z) ? -_w.GetValueFloat(SO) : _w.GetValueFloat(SO); }
      set { _w.SetValue(SO, (_base.CenterOfTurnZ < Position.Z) ? -value : value); }
    }
    public float Strength {
      get { return _w.Strength; }
      set { _w.Strength = value; }
    }
    public readonly int WheelSize;

    readonly WheelBase _base;
    readonly float _minY, _maxY;
    readonly bool _revPow;
    readonly bool _revY;
    readonly CoordsTransformer _tform;
    readonly IMyMotorSuspension _w;
    int _prevRoll;

    public PowerWheel(IMyMotorSuspension w, WheelBase wb, CoordsTransformer tform) {
      _base = wb;
      CubeSize = w.CubeGrid.GridSizeEnum;
      Position = tform.Pos(w.GetPosition());
      _revPow = RIGHT.Dot(tform.Dir(w.WorldMatrix.Up)) > 0;
      _revY = UP.Dot(tform.Dir(w.WorldMatrix.Backward)) > 0;
      _tform = tform;
      _w = w;

      w.Height = 10;
      _maxY = w.Height;
      w.Height = -10;
      _minY = w.Height;
      if (_revY) {
        float tmp = _minY;
        _minY = -_maxY;
        _maxY = -tmp;
      }
      WheelSize = (w.Top.Max - w.Top.Min).X + 1;
      Mass = CubeSize == MyCubeSize.Small
        ? WheelSize == 1 ? 105 : (WheelSize == 3 ? 205 : 310)
        : WheelSize == 1 ? 420 : (WheelSize == 3 ? 590 : 760);
      // real center of rotation of the wheel
      Position += tform.Dir(w.WorldMatrix.Up) * w.CubeGrid.GridSize;
      _base.AddWheel(this);
    }

    public void Strafe(double comPos) {
      _w.SetValue("MaxSteerAngle", 30f);
      _w.InvertSteer = comPos < Position.Z;
    }

    public void Turn(double comPos) {
      bool turnLeft = _w.SteerAngle < 0;
      float angle = _base.GetAngle(this, turnLeft);
      if (Math.Abs(_w.MaxSteerAngle - angle) > 0.01) {
        _w.SetValue("MaxSteerAngle", angle);
        _w.InvertSteer = (comPos < Position.Z) ^ (_base.CenterOfTurnZ < Position.Z);
      }
    }

    public void Roll(float roll) {
      int iRoll = (int)(5 * MathHelper.Clamp(roll * 4, -1, 1));
      if (iRoll != _prevRoll) {
        _prevRoll = iRoll;
        if((iRoll > 0 && _revPow) || (iRoll < 0 && !_revPow))
          _w.Height = (_revY ? -_maxY : _minY) + ((Math.Abs(iRoll) + 2) * 0.04f);
        else
          _w.Height = _revY ? -_maxY : _minY;
      }
    }

    public float GetCompressionRatio() {
      double pos = _tform.Pos(_w.Top.GetPosition()).Y - _tform.Pos(_w.GetPosition()).Y;
      return ((float)(_revY ? -pos : pos) - _minY) / (_maxY - _minY);
    }

    public Vector3D GetPointOfContactW() => _w.Top.GetPosition() +
        (_radius() * (_revY ? _w.WorldMatrix.Forward : _w.WorldMatrix.Backward));

    double _radius() => WheelSize * (CubeSize == MyCubeSize.Large ? 1.25 : 0.25);
  }
}
}
