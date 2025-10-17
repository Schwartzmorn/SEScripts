namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using Utilities.Mocks.Base;
using VRageMath;

public class MyCameraBlockMock(MyCubeGridMock cubeGridMock) : MyFunctionalBlockMock(cubeGridMock), IMyCameraBlock
{
  public bool IsActive => throw new System.NotImplementedException();

  public double AvailableScanRange => throw new System.NotImplementedException();

  public bool EnableRaycast { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

  public float RaycastConeLimit => throw new System.NotImplementedException();

  public double RaycastDistanceLimit => throw new System.NotImplementedException();

  public float RaycastTimeMultiplier => throw new System.NotImplementedException();

  public bool CanScan(double distance)
  {
    throw new System.NotImplementedException();
  }

  public bool CanScan(double distance, Vector3D direction)
  {
    throw new System.NotImplementedException();
  }

  public bool CanScan(Vector3D target)
  {
    throw new System.NotImplementedException();
  }

  public MyDetectedEntityInfo Raycast(double distance, float pitch = 0, float yaw = 0)
  {
    throw new System.NotImplementedException();
  }

  public MyDetectedEntityInfo Raycast(Vector3D targetPos)
  {
    throw new System.NotImplementedException();
  }

  public MyDetectedEntityInfo Raycast(double distance, Vector3D targetDirection)
  {
    throw new System.NotImplementedException();
  }

  public override void Tick()
  {
  }

  public int TimeUntilScan(double distance)
  {
    throw new System.NotImplementedException();
  }
}
