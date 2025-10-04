using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace Utilities.Mocks.Base;

public abstract class MyMechanicalConnectionBlockMock(MyCubeGridMock cubeGridMock) : MyFunctionalBlockMock(cubeGridMock), IMyMechanicalConnectionBlock
{

  public IMyCubeGrid TopGrid => Top?.CubeGrid;

  public IMyAttachableTopBlock Top { get; set; }

  public float SafetyLockSpeed { get; set; }
  public bool SafetyLock { get; set; }

  public bool IsAttached => Top != null;

  public IMyAttachableTopBlock PendingAttachementBlock { get; set; }

  public bool PendingAttachment => PendingAttachementBlock != null;

  public void Attach()
  {
    if (!IsAttached && PendingAttachment)
    {
      Top = PendingAttachementBlock;
      (Top as MyAttachableBlockMock).Base = this;
    }
  }

  public void Detach()
  {
    PendingAttachementBlock = Top;
    if (Top != null)
    {
      (Top as MyAttachableBlockMock).Base = null;
      Top = null;
    }
  }

  public abstract bool IsLocked { get; }
}
