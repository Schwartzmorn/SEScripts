using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
  partial class Program {
    /// <summary>Classes that watches a block for updates to its custom data, and updates the different consumers with the result</summary>
    public class IniWatcher: MyIni {
      public static readonly char[] SEP = new char[] { ',' };

      private readonly IMyTerminalBlock block;
      private readonly List<IIniConsumer> consumers = new List<IIniConsumer>();
      private string previous;

      /// <summary>Creates the watcher</summary>
      /// <param name="b">Block whose <see cref="IMyTerminalBlock.CustomData"/> we're intersted in</param>
      /// <param name="spawner">To spawn the watch process</param>
      public IniWatcher(IMyTerminalBlock b, IProcessSpawner spawner) {
        this.block = b;
        this.Parse(b.CustomData);
        this.previous = this.block.CustomData;
        spawner.Spawn(this.update, "ini-update", period: 100);
      }

      public void Add(IIniConsumer c) => this.consumers.Add(c);

      private void update(Process p) {
        if (!ReferenceEquals(this.block.CustomData, this.previous)) {
          this.previous = this.block.CustomData;
          this.Parse(this.previous);
          this.consumers.ForEach(c => c.Read(this));
        }
      }
    }
  }
}
