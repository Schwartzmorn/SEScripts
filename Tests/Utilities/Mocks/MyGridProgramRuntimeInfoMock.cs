namespace Utilities.Mocks;

using Sandbox.ModAPI.Ingame;
using System;

/// <summary>
/// A mock implementation of <see cref="IMyGridProgramRuntimeInfo"/> for testing purposes.
/// </summary>
public class MyGridProgramRuntimeInfoMock : IMyGridProgramRuntimeInfo
{
  // TODO properly count ticks
  public long LifetimeTicks { get; set; } = 0;

  public TimeSpan TimeSinceLastRun => new(0);

  public double LastRunTimeMs => TimeSinceLastRun.TotalMilliseconds;

  public int MaxInstructionCount => 50000;

  public int CurrentInstructionCount => 0;

  public int MaxCallChainDepth => 1000;

  public int CurrentCallChainDepth => 0;

  public UpdateFrequency UpdateFrequency { get; set; }
}
