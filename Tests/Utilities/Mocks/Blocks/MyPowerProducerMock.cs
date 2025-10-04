namespace Utilities.Mocks.Base;

using Sandbox.ModAPI.Ingame;

public abstract class MyPowerProducerMock(MyCubeGridMock cubeGridMock) : MyFunctionalBlockMock(cubeGridMock), IMyPowerProducer
{
  public abstract string ProducerType { get; }

  public float CurrentOutput { get; set; }

  public float MaxOutput { get; set; }

  public float CurrentOutputRatio { get => CurrentOutput / MaxOutput; }

  public new string DetailedInfo
  {
    get
    {
      return $@"Type: {ProducerType}
Max Output: {MaxOutput.FormatAmount("W")}
Current Output: {CurrentOutput.FormatAmount("W")}";
    }
  }

  public override void Tick()
  {
  }
}
