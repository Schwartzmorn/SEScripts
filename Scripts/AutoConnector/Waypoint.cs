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
    public enum ConnectionType {
      None,
      Front,
      Down
    }
    public class Waypoint {
      public readonly Vector3D Position;
      public readonly float Angle;
      public readonly ConnectionType Connection;
      public readonly bool NeedPrecision;

      public Waypoint(Vector3D position, float angle = 0, ConnectionType connection = ConnectionType.None, bool needPrecision = false) {
        Position = position;
        Angle = angle;
        Connection = connection;
        NeedPrecision = needPrecision;
      }
    }
  }
}
