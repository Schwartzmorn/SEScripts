namespace Utilities.Mocks.Base;

using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

public class MyRemoteControlMock(MyCubeGridMock myCubeGridMock) : MyShipControllerMock(myCubeGridMock), IMyRemoteControl
{
  public List<MyWaypointInfo> Waypoints { get; set; } = [];

  public bool IsAutoPilotEnabled { get; private set; }

  public float SpeedLimit { get; set; }
  public FlightMode FlightMode { get; set; }
  public Base6Directions.Direction Direction { get; set; }

  public MyWaypointInfo CurrentWaypoint => throw new System.NotImplementedException();

  public bool WaitForFreeWay { get; set; }

  public void AddWaypoint(Vector3D coords, string name)
  {
    AddWaypoint(new MyWaypointInfo(name, coords));
  }

  public void AddWaypoint(MyWaypointInfo coords)
  {
    Waypoints.Add(coords);
  }

  public void ClearWaypoints()
  {
    Waypoints.Clear();
  }

  public bool GetNearestPlayer(out Vector3D playerPosition)
  {
    throw new System.NotImplementedException();
  }

  public void GetWaypointInfo(List<MyWaypointInfo> waypoints)
  {
    waypoints.AddRange(Waypoints);
  }

  public void SetAutoPilotEnabled(bool enabled)
  {
  }

  public void SetCollisionAvoidance(bool enabled)
  {
    throw new System.NotImplementedException();
  }

  public void SetDockingMode(bool enabled)
  {
    throw new System.NotImplementedException();
  }
}
