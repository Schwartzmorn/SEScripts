using Sandbox.ModAPI.Ingame;

namespace Utilities.Mocks.Base;

public class MyAttachableBlockMock : MyCubeBlockMock, IMyAttachableTopBlock
{
  public MyAttachableBlockMock(MyCubeGridMock myCubeGridMock, MyMechanicalConnectionBlockMock baseBlock = null) : base(myCubeGridMock)
  {
    if (baseBlock != null)
    {
      Base = baseBlock;
      baseBlock.Top = this;
    }
  }

  public bool IsAttached => Base != null;

  public IMyMechanicalConnectionBlock Base { get; set; }
}
