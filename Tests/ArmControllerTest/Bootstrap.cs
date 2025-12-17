using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript;

public partial class Program : MyGridProgram
{
  CommandLine _cmd;
  IProcessManager _manager;

  public ArmController Controller { get; init; }

  public Program()
  {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    IMyShipController cockpit = GridTerminalSystem.GetBlockWithName("Cockpit") as IMyShipController;
    _manager = Process.CreateManager(Echo);
    var ct = new CoordinatesTransformer(cockpit, _manager);
    _cmd = new CommandLine("Arm controller", Echo, _manager);
    var ini = new IniWatcher(Me, _manager);
    var wc = new WheelsController(_cmd, cockpit, GridTerminalSystem, ini, _manager, ct);
    Controller = new ArmController(ini, this, _cmd, cockpit, wc, _manager);
  }

  public void Save() => _manager.Save(s => Me.CustomData = s);

  public void Main(string arg, UpdateType us)
  {
    _cmd.StartCmd(arg, CommandTrigger.User);
    if ((us & UpdateType.Update1) > 0)
    {
      _manager.Tick();
    }
  }
}
