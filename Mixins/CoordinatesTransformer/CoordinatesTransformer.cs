using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Class that transforms coordinates from the world coordinates to an object's coordinates</summary>
    public class CoordinatesTransformer
    {
      readonly IMyTerminalBlock _reference;
      Vector3D _position;
      MatrixD _dirMatrix;
      /// <summary>Creates a new transformer</summary>
      /// <param name="block">Reference block, coordinates will then be </param>
      /// <param name="spawner">If present, a <see cref="Process"/> will be spawned to update the transformer each tick</param>
      public CoordinatesTransformer(IMyTerminalBlock block, IProcessSpawner spawner = null)
      {
        _reference = block;
        _update(null);
        spawner?.Spawn(_update, $"coords-transformer {_reference.CustomName}");
      }
      /// <summary>Returns the coordinates <paramref name="pos"/> in the transformer's referential.</summary>
      /// <param name="pos">Position to transform</param>
      /// <returns>The transformed coordinates</returns>
      public Vector3D Pos(Vector3D pos) => Vector3D.TransformNormal(pos - _position, _dirMatrix);
      /// <summary>Returns the direction <paramref name="dir"/> in the transformer's referential.</summary>
      /// <param name="dir">Direction to transform</param>
      /// <returns>The transformed direction</returns>
      public Vector3D Dir(Vector3D dir) => Vector3D.TransformNormal(dir, _dirMatrix);
      void _update(Process p)
      {
        _position = _reference.GetPosition();
        _dirMatrix = MatrixD.Transpose(_reference.WorldMatrix);
      }
    }
  }
}
