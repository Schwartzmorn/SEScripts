using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
  /// <summary>
  /// cool idea, bro
  /// </summary>
    public class ControlBinder {
      static readonly HashSet<string> ACTIONS = new HashSet<string>{"rollleft", "rollright", "forward", "backward", "left", "right", "up", "down"};
      readonly List<IMyShipController> shipControllers;
      readonly Dictionary<string, Action> actions;
      Dictionary<string, float> previous;

      public ControlBinder(List<IMyShipController> shipControllers, ISaveManager spawner) {
        this.shipControllers = shipControllers;
        spawner.Spawn(p => this.handle(), "control-binder");
        this.previous = ACTIONS.ToDictionary(s => s, s => 0f);
      }

      public void AddAction(string s, Action action) {
        this.actions[s] = action;
      }

      void handle(){
        var newState = new Dictionary<string, float>();
        newState["rollleft"] = this.shipControllers.Sum(c => c.RollIndicator);
        newState["rollright"] = -newState["rollleft"];
        newState["forward"] = this.shipControllers.Sum(c => c.MoveIndicator.X);
        newState["backward"] = -newState["forward"];
        newState["left"] = this.shipControllers.Sum(c => c.MoveIndicator.Z);
        newState["right"] = -newState["left"];
        newState["up"] = this.shipControllers.Sum(c => c.MoveIndicator.Y);
        newState["down"] = -newState["up"];
        foreach(var kv in newState) {
          newState[kv.Key] = Math.Max(0, kv.Value);
        }
        foreach (string action in ACTIONS) {
          
        }
      }
    }
  }
}
