namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using Utilities.Mocks.Base;

public class MyShipConnectorMock(MyCubeGridMock cubeGridMock) : MyFunctionalBlockMock(cubeGridMock), IMyShipConnector
{
  public bool ThrowOut { get; set; }
  public bool CollectAll { get; set; }
  public float PullStrength { get; set; }

  public bool IsLocked => throw new System.NotSupportedException("Obsolete, use IsConnected instead");

  public bool IsConnected => throw new System.NotImplementedException();

  public MyShipConnectorMock PendingOtherConnector { get; set; }

  public MyShipConnectorStatus Status
  {
    get
    {
      if (OtherConnector != null)
      {
        return MyShipConnectorStatus.Connected;
      }
      if (PendingOtherConnector != null)
      {
        return MyShipConnectorStatus.Connectable;
      }
      return MyShipConnectorStatus.Unconnected;
    }
  }

  public IMyShipConnector OtherConnector { get; private set; }

  public bool IsParkingEnabled { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

  public void Connect()
  {
    OtherConnector = PendingOtherConnector;
  }

  public void Disconnect()
  {
    PendingOtherConnector = OtherConnector as MyShipConnectorMock;
    OtherConnector = null;
  }

  public override void Tick()
  {
  }

  public void ToggleConnect()
  {
    if (Status == MyShipConnectorStatus.Connected)
    {
      Disconnect();
    }
    else
    {
      Connect();
    }
  }
}
