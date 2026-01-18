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

namespace IngameScript
{
  partial class Program
  {
    public enum Terrain { Normal = 0, Dangerous, Bad, Good, Open }
    /// <summary>Class that wraps a Waypoint coordinates, some information about the terrain around and to what other waypionts it is connected to</summary>
    public class APWaypoint
    {
      public MyWaypointInfo WP;
      public readonly Terrain Terrain = Terrain.Normal;
      public readonly List<APWaypoint> LinkedWps = new List<APWaypoint>();
      public Vector3D Coords => WP.Coords;
      public string Name => WP.Name;

      // A* search
      public double DistFromStart { get; private set; }
      public double DistToGoal { get; private set; }
      public APWaypoint PrevWP { get; private set; }
      public bool Visited { get; private set; }

      List<string> _waypointsNames;
      readonly static Log LOG = Log.GetLog("WP");

      double? _aStartDistance;
      /// <summary>Create a new waypoint, not connected to any other waypoint</summary>
      /// <param name="wp">Coordinates and name of the waypoint</param>
      /// <param name="terrain">Information about how rough the terrain is</param>
      /// <param name="type">How close to the waypoint the autopilot should try to be</param>
      public APWaypoint(MyWaypointInfo wp, Terrain terrain = Terrain.Normal)
      {
        WP = wp;
        Terrain = terrain;
      }
      /// <summary>Links this waypoint to another waypoint, denoting the autopilot can go from <see cref="this"/> to <paramref name="wp"/>. It is not bidirectionnal.</summary>
      /// <param name="wp">Waypoint reachable from this waypoint.</param>
      public void AddWP(APWaypoint wp)
      {
        if (_waypointsNames == null)
        {
          _waypointsNames = new List<string>();
        }
        _waypointsNames.Add(wp.Name);
        LinkedWps.Add(wp);
      }
      /// <summary>Creates a new waypoint from an ini string, possibly connected to other waypoints</summary>
      /// <param name="iniString">ini that contains the waypoints</param>
      /// <param name="section">name of the section containing information about the waypoints</param>
      public APWaypoint(string name, string iniString)
      {
        var iniParts = iniString.Split(':');
        double x, y, z;
        double.TryParse(iniParts[0], out x);
        double.TryParse(iniParts[1], out y);
        double.TryParse(iniParts[2], out z);
        WP = new MyWaypointInfo(name, x, y, z);
        Enum.TryParse(iniParts[3], out Terrain);
        _waypointsNames = iniParts[4].Split(IniHelper.SEP, StringSplitOptions.RemoveEmptyEntries).ToList();
      }
      /// <summary>Saves the parameters of this waypoint to an ini string</summary>
      /// <param name="ini">Where the waypoint will be saved</param>
      public void Save(MyIni ini)
      {
        var waypoints = _waypointsNames == null ? "" : string.Join(",", _waypointsNames);
        ini.Set("gps", Name, $"{WP.Coords.X:0.00}:{WP.Coords.Y:0.00}:{WP.Coords.Z:0.00}:${(Terrain == Terrain.Normal ? "" : Terrain.ToString())}:${waypoints}");
      }
      /// <summary>Translates the list of waypoint names from the ini string to a list of actual <see cref="APWaypoint"/></summary>
      /// <param name="network">Network that contains all the waypoints</param>
      public void Update(WPNetwork network)
      {
        if (_waypointsNames != null)
        {
          int notFound = 0;
          foreach (string wpName in _waypointsNames)
          {
            APWaypoint wp = network.GetWaypoint(wpName);
            if (wp == null)
            {
              ++notFound;
            }
            else
            {
              LinkedWps.Add(wp);
            }
          }
          if (notFound != 0)
          {
            LOG.Error($"Waypoint '{Name}' links to {notFound} unknown waypoints");
          }
        }
      }
      /// <summary>For A* search</summary>
      public void ResetAStar()
      {
        DistFromStart = double.MaxValue;
        DistToGoal = double.MaxValue;
        PrevWP = null;
        Visited = false;
        _aStartDistance = null;
      }
      /// <summary>For A* search</summary>
      public void SetAStarPrev(APWaypoint prev, APWaypoint end, double fromStart)
      {
        DistToGoal = fromStart + _getAStarDist(end);
        PrevWP = prev;
        DistFromStart = fromStart;
      }
      /// <summary>For A* search</summary>
      public void Visit() => Visited = true;

      double _getAStarDist(APWaypoint a)
      {
        if (_aStartDistance == null)
        {
          _aStartDistance = (WP.Coords - a.WP.Coords).Length();
        }
        return _aStartDistance.Value;
      }
    }
  }
}
