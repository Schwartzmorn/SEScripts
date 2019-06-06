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
  public enum WPType { Path = 0, PrecisePath, Maneuvering }
  public enum Terrain { Normal = 0, Dangerous, Bad, Good, Open }

  public class APWaypoint {
    public MyWaypointInfo WP;
    public readonly Terrain Terrain = Terrain.Normal;
    public readonly WPType Type = WPType.Path;
    public readonly List<APWaypoint> LinkedWps = new List<APWaypoint>();
    public string Name => WP.Name;

    // A* search
    public double DistFromStart { get; private set; }
    public double DistToGoal { get; private set; }
    public APWaypoint PrevWP { get; private set; }
    public bool Visited { get; private set; }

    List<string> _wps;

    double? _asDist;

    public APWaypoint(MyWaypointInfo wp, Terrain terrain = Terrain.Normal, WPType type = WPType.Path) {
      WP = wp;
      Terrain = terrain;
      Type = type;
    }

    public void AddWP(APWaypoint wp) {
      if (_wps == null)
        _wps = new List<string>();
      _wps.Add(wp.Name);
      LinkedWps.Add(wp);
    }

    public APWaypoint(Ini ini, string section) {
      MyWaypointInfo.TryParse(ini.Get(section, "gps").ToString(), out WP);
      Enum.TryParse(ini.Get(section, "type").ToString(), out Type);
      Enum.TryParse(ini.Get(section, "terrain").ToString(), out Terrain);
      _wps = ini.Get(section, "linked-wp").ToString().Split(Ini.SEP, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public void Save(MyIni ini) {
      ini.Set(Name, "gps", $"GPS:{Name}:{WP.Coords.X:0.00}:{WP.Coords.Y:0.00}:{WP.Coords.Z:0.00}:");
      if (Terrain != Terrain.Normal)
        ini.Set(Name, "terrain", Terrain.ToString());
      if (Type != WPType.Path)
        ini.Set(Name, "type", Type.ToString());
      if (_wps != null)
        ini.Set(Name, "linked-wp", string.Join(",", _wps));
    }

    public void Update(WPNetwork network) {
      if (_wps != null) {
        int notFound = 0;
        foreach (string wpName in _wps) {
          var wp = network.GetWP(wpName);
          if (wp == null)
            ++notFound;
          else
            LinkedWps.Add(wp);
        }
        if (notFound != 0) {
          Log($"Waypoint '{Name}' links to {notFound} unknown waypoints");
        }
      }
    }

    public void ResetAS() {
      DistFromStart = double.MaxValue;
      DistToGoal = double.MaxValue;
      PrevWP = null;
      Visited = false;
      _asDist = null;
    }

    public void SetASPrev(APWaypoint prev, APWaypoint end, double fromStart) {
      DistToGoal = fromStart + _getASDist(end);
      PrevWP = prev;
      DistFromStart = fromStart;
    }

    public void Visit() => Visited = true;

    double _getASDist(APWaypoint a) {
      if (_asDist == null)
        _asDist = (WP.Coords - a.WP.Coords).Length();
      return _asDist.Value;
    }
  }
}
}
