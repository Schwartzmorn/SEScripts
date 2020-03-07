using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    /// <summary>Small class that helps with the serialization of commands</summary>
    public class CommandSerializer {
      readonly StringBuilder builder;
      /// <summary>Creates a new serializer</summary>
      /// <param name="command">Name of the command to execute</param>
      public CommandSerializer(string command) {
        this.builder = new StringBuilder($"-{command}");
      }
      /// <summary>Add the <see cref="ToString"/> result of the object as argument. Takes care of wrapping it in quotes.</summary>
      /// <param name="o">Object to add as argument</param>
      /// <returns>itself</returns>
      public CommandSerializer AddArg(object o) {
        this.builder.Append($" \"{o.ToString()}\"");
        return this;
      }
      /// <summary>Returns the serialized command</summary>
      /// <returns>The serailized command</returns>
      public override string ToString() => this.builder.ToString();
    }
  }
}
