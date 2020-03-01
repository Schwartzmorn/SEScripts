using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public class CoordsTransformer
    {
      readonly IMyTerminalBlock _ref;
      Vector3D _p;
      MatrixD _m;
      public CoordsTransformer(IMyTerminalBlock block, bool schedule)
      {
        _ref = block;
        _upd();
        if (schedule)
          Schedule(new ScheduledAction(_upd, name: $"ct-upd-{_ref.DisplayNameText}"));
      }
      public Vector3D Pos(Vector3D pos) => Vector3D.TransformNormal(pos - _p, _m);
      public Vector3D Dir(Vector3D dir) => Vector3D.TransformNormal(dir, _m);
      void _upd()
      {
        _p = _ref.GetPosition();
        _m = MatrixD.Transpose(_ref.WorldMatrix);
      }
    }
  }
}
