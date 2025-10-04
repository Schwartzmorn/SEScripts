namespace Utilities;

using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using Utilities.Mocks.Blocks;

public class ProgramWrapper
{
  public List<string> EchoMessages { get; } = [];

  public MyGridProgram Program { get; }

  public MyGridProgramRuntimeInfoMock RuntimeMock => Program.Runtime as MyGridProgramRuntimeInfoMock;

  public MyGridTerminalSystemMock GridTerminalSystemMock => Program.GridTerminalSystem as MyGridTerminalSystemMock;

  public MyIntergridCommunicationSystemMock IntergridCommunicationSystemMock => Program.IGC as MyIntergridCommunicationSystemMock;

  /// <summary>
  ///  Returns the Grid where the programmable block is
  /// </summary>
  public MyCubeGridMock CubeGridMock => Program.Me.CubeGrid as MyCubeGridMock;

  /// <summary>
  ///  Returns the programmable block
  /// </summary>
  public MyProgrammableBlockMock ProgrammableBlockMock => Program.Me as MyProgrammableBlockMock;

  public Action Initialize;

  public ProgramWrapper(MyCubeGridMock cubeGrid, Type P)
  {
    var constructor = P.GetConstructor(Type.EmptyTypes) ?? throw new InvalidOperationException("No parameterless constructor found.");
    Program = RuntimeHelpers.GetUninitializedObject(P) as MyGridProgram;
    if (Program is not IMyGridProgram backend)
      throw new InvalidOperationException("No IMyGridProgram interface found.");

    backend.Runtime = new MyGridProgramRuntimeInfoMock();
    backend.Storage = "";
    backend.GridTerminalSystem = cubeGrid.GridTerminalSystem;
    backend.Echo = EchoMessages.Add;
    backend.Me = new MyProgrammableBlockMock(cubeGrid, Program);
    var igc = new MyIntergridCommunicationSystemMock(cubeGrid.GridTerminalSystem.TestBed);
    backend.IGC_ContextGetter = () => igc;

    // We defer the call to the constructor to the first Tick to allow the tests to Initialize the grid blocks first
    Initialize = () => constructor.Invoke(Program, null);
  }
}
public class TestBed
{
  public ProgramWrapper ProgramWrapper { get; private set; }

  private long _nextEntityId = 1;

  public long GetNextEntityId() => _nextEntityId++;

  public TestBed()
  {
  }

  public ProgramWrapper CreateProgram<T>() where T : MyGridProgram, IMyGridProgram, new()
  {
    var gts = new MyGridTerminalSystemMock(this);
    var grid = new MyCubeGridMock(gts);

    ProgramWrapper = new ProgramWrapper(grid, typeof(T));
    return ProgramWrapper;
  }

  public void Tick()
  {
    if (ProgramWrapper.RuntimeMock.LifetimeTicks == 0)
    {
      Console.WriteLine("Initializing program");
      ProgramWrapper.Initialize();
    }

    ProgramWrapper.RuntimeMock.LifetimeTicks++;

    ProgramWrapper.GridTerminalSystemMock.CubeGridMocks.SelectMany(g => g.TerminalBlockMocks).ForEach(b => b.Tick());
  }

  public void Tick(int count)
  {
    for (int i = 0; i < count; i++)
    {
      Tick();
    }
  }
}
