namespace Utilities.Mocks.Blocks;

using Sandbox.ModAPI.Ingame;
using System;
using System.Reflection;
using Utilities.Mocks.Base;

public class MyProgrammableBlockMock : MyFunctionalBlockMock, IMyProgrammableBlock, IMyTextSurfaceProviderMock
{
  public MyProgrammableBlockMock(MyCubeGridMock cubeGridMock, MyGridProgram program) : base(cubeGridMock)
  {
    Program = program;
    CustomName = "Programmable Block " + EntityId;
    // TODO add surfaces
  }

  public MyGridProgram Program { get; }

  public MyTextSurfaceProviderImpl SurfaceProviderImpl { get; } = new MyTextSurfaceProviderImpl();

  public bool IsRunning { get; private set; }

  public string TerminalRunArgument { get; set; }

  public bool TryRun(string argument)
  {
    throw new NotImplementedException();
  }

  public override void Tick()
  {
    UpdateType updateType = UpdateType.None;

    if (Program.Runtime.UpdateFrequency.HasFlag(UpdateFrequency.Update1))
    {
      updateType |= UpdateType.Update1;
    }
    if (Program.Runtime.LifetimeTicks % 10 == 0 && Program.Runtime.UpdateFrequency.HasFlag(UpdateFrequency.Update10))
    {
      updateType |= UpdateType.Update10;
    }
    if (Program.Runtime.LifetimeTicks % 100 == 0 && Program.Runtime.UpdateFrequency.HasFlag(UpdateFrequency.Update100))
    {
      updateType |= UpdateType.Update100;
    }

    if (updateType == UpdateType.None)
    {
      return;
    }

    IsRunning = true;
    _getMain().Invoke(Program, ["", updateType]);
    IsRunning = false;
  }

  private MethodInfo _getMain()
  {
    Type type = Program.GetType();
    MethodInfo method = type.GetMethod("Main", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
    [
      typeof(string),
      typeof(UpdateType)
    ], null) ?? throw new InvalidOperationException("No suitable Main method found. Your program must have a Main method with the signature 'void Main(string argument, UpdateType updateSource)'.");
    return method;
  }
}
