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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>
    /// cool idea, bro
    /// </summary>
    public class ControlBinder
    {
      static readonly HashSet<string> ACTIONS = new HashSet<string> { "rollleft", "rollright", "forward", "backward", "left", "right", "up", "down" };
      readonly List<IMyShipController> _shipControllers;
      readonly Dictionary<string, Action> _actions;
      Dictionary<string, float> _previous;

      public ControlBinder(List<IMyShipController> shipControllers, ISaveManager spawner)
      {
        _shipControllers = shipControllers;
        spawner.Spawn(p => _handle(), "control-binder");
        _previous = ACTIONS.ToDictionary(s => s, s => 0f);
      }

      public void AddAction(string s, Action action)
      {
        _actions[s] = action;
      }

      void _handle()
      {
        var newState = new Dictionary<string, float>();
        newState["rollleft"] = _shipControllers.Sum(c => c.RollIndicator);
        newState["rollright"] = -newState["rollleft"];
        newState["forward"] = _shipControllers.Sum(c => c.MoveIndicator.X);
        newState["backward"] = -newState["forward"];
        newState["left"] = _shipControllers.Sum(c => c.MoveIndicator.Z);
        newState["right"] = -newState["left"];
        newState["up"] = _shipControllers.Sum(c => c.MoveIndicator.Y);
        newState["down"] = -newState["up"];
        foreach (var kv in newState)
        {
          newState[kv.Key] = Math.Max(0, kv.Value);
        }
        foreach (string action in ACTIONS)
        {

        }
      }
    }
  }
}
