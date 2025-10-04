using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Classes that watches a block for updates to its custom data, and updates the different consumers with the result</summary>
    public class IniWatcher : MyIni
    {
      readonly IMyTerminalBlock _block;
      readonly List<IIniConsumer> _consumers = new List<IIniConsumer>();
      string _previous;
      /// <summary>Creates the watcher</summary>
      /// <param name="b">Block whose <see cref="IMyTerminalBlock.CustomData"/> we're intersted in</param>
      /// <param name="spawner">To spawn the watch process</param>
      public IniWatcher(IMyTerminalBlock b, IProcessSpawner spawner)
      {
        _block = b;
        this.Parse(b.CustomData);
        _previous = _block.CustomData;
        spawner.Spawn(_update, "ini-update", period: 100);
      }
      public void Add(IIniConsumer c) => _consumers.Add(c);
      void _update(Process p)
      {
        if (!ReferenceEquals(_block.CustomData, _previous))
        {
          _previous = _block.CustomData;
          this.Parse(_previous);
          _consumers.ForEach(c => c.Read(this));
        }
      }
    }
  }
}
