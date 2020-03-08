using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Class that transforms coordinates from the world coordinates to an object's coordinates</summary>
    public class CoordinatesTransformer
    {
      readonly IMyTerminalBlock reference;
      Vector3D position;
      MatrixD directionMatrix;
      /// <summary>Creates a new transformer</summary>
      /// <param name="block">Reference block, coordinates will then be </param>
      /// <param name="spawner">If present, a <see cref="Process"/> will be spawned to update the transformer each tick</param>
      public CoordinatesTransformer(IMyTerminalBlock block, IProcessSpawner spawner = null)
      {
        this.reference = block;
        this.update(null);
        spawner?.Spawn(this.update, $"coords-transformer {this.reference.DisplayNameText}");
      }
      /// <summary>Returns the coordinates <paramref name="pos"/> in the transformer's referential.</summary>
      /// <param name="pos">Position to transform</param>
      /// <returns>The transformed coordinates</returns>
      public Vector3D Pos(Vector3D pos) => Vector3D.TransformNormal(pos - this.position, this.directionMatrix);
      /// <summary>Returns the direction <paramref name="dir"/> in the transformer's referential.</summary>
      /// <param name="dir">Direction to transform</param>
      /// <returns>The transformed direction</returns>
      public Vector3D Dir(Vector3D dir) => Vector3D.TransformNormal(dir, this.directionMatrix);
      void update(Process p)
      {
        this.position = this.reference.GetPosition();
        this.directionMatrix = MatrixD.Transpose(this.reference.WorldMatrix);
      }
    }
  }
}
