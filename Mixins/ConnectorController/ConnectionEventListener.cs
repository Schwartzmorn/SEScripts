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
    public interface IConnectionEventListener {
      void OnStart(bool isConnection);
      // progress is [0 - 1]
      void OnProgress(bool isConnection, float progress);
      void OnDone(bool isConnection);
      void OnCancel(bool isConnection, bool byClient);
      void OnTimeout(bool isConnection);
    }
  }
}
