namespace Utilities.Mocks.Blocks;

using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Utilities.Mocks.Base;

class MySensorBlock(MyCubeGridMock cubeGridMock) : MyFunctionalBlockMock(cubeGridMock), IMySensorBlock
{
  public float MaxRange => throw new System.NotImplementedException();

  public float LeftExtend { get; set; }
  public float RightExtend { get; set; }
  public float TopExtend { get; set; }
  public float BottomExtend { get; set; }
  public float FrontExtend { get; set; }
  public float BackExtend { get; set; }
  public bool PlayProximitySound { get; set; }
  public bool DetectPlayers { get; set; }
  public bool DetectFloatingObjects { get; set; }
  public bool DetectSmallShips { get; set; }
  public bool DetectLargeShips { get; set; }
  public bool DetectStations { get; set; }
  public bool DetectSubgrids { get; set; }
  public bool DetectAsteroids { get; set; }
  public bool DetectOwner { get; set; }
  public bool DetectFriendly { get; set; }
  public bool DetectNeutral { get; set; }
  public bool DetectEnemy { get; set; }

  public bool IsActive { get; set; }

  public MyDetectedEntityInfo LastDetectedEntity => throw new System.NotImplementedException();

  public void DetectedEntities(List<MyDetectedEntityInfo> entities)
  {
    throw new System.NotImplementedException();
  }

  public override void Tick()
  {
  }
}
