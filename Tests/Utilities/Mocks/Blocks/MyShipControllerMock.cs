using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace Utilities.Mocks.Base;

public class MyShipControllerMock(MyCubeGridMock myCubeGridMock) : MyTerminalBlockMock(myCubeGridMock), IMyShipController
{
  public bool CanControlShip { get; set; }

  public bool IsUnderControl { get; set; }

  public bool HasWheels => throw new System.NotImplementedException();

  public bool ControlWheels { get; set; }
  public bool ControlThrusters { get; set; }
  public bool HandBrake { get; set; }
  public bool DampenersOverride { get; set; }
  public bool ShowHorizonIndicator { get; set; }

  public Vector3 MoveIndicator => throw new System.NotImplementedException();

  public Vector2 RotationIndicator => throw new System.NotImplementedException();

  public float RollIndicator => throw new System.NotImplementedException();

  public Vector3D CenterOfMass { get; set; }

  public bool IsMainCockpit { get; set; }

  public MyShipMass ShipMassMock { get; set; }

  public MyShipMass CalculateShipMass() => ShipMassMock;

  public Vector3D ArtificialGravityMock { get; set; } = default;
  public Vector3D GetArtificialGravity() => ArtificialGravityMock;

  public Vector3D NaturalGravityMock { get; set; } = new Vector3D(0, -1, 0);
  public Vector3D GetNaturalGravity() => NaturalGravityMock;

  public double GetShipSpeed() => GetShipVelocities().LinearVelocity.Length();

  public MyShipVelocities ShipVelocitiesMock { get; set; }
  public MyShipVelocities GetShipVelocities() => ShipVelocitiesMock;

  public Vector3D GetTotalGravity() => GetArtificialGravity() + GetNaturalGravity();

  public bool TryGetPlanetElevation(MyPlanetElevation detail, out double elevation)
  {
    throw new System.NotImplementedException();
  }

  public bool TryGetPlanetPosition(out Vector3D position)
  {
    throw new System.NotImplementedException();
  }

  public override void Tick()
  {
  }
}
