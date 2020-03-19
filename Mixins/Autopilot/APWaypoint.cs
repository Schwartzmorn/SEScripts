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
    /// <summary>Class that wraps a Waypoint coordinates, some information about the terrain around and to what other waypionts it is connected to</summary>
    public class APWaypoint {
      public MyWaypointInfo WP;
      public readonly Terrain Terrain = Terrain.Normal;
      public readonly WPType Type = WPType.Path;
      public readonly List<APWaypoint> LinkedWps = new List<APWaypoint>();
      public Vector3D Coords => this.WP.Coords;
      public string Name => this.WP.Name;

      // A* search
      public double DistFromStart { get; private set; }
      public double DistToGoal { get; private set; }
      public APWaypoint PrevWP { get; private set; }
      public bool Visited { get; private set; }

      List<string> waypointsNames;

      double? aStartDistance;
      /// <summary>Create a new waypoint, not connected to any other waypoint</summary>
      /// <param name="wp">Coordinates and name of the waypoint</param>
      /// <param name="terrain">Information about how rough the terrain is</param>
      /// <param name="type">How close to the waypoint the autopilot should try to be</param>
      public APWaypoint(MyWaypointInfo wp, Terrain terrain = Terrain.Normal, WPType type = WPType.Path) {
        this.WP = wp;
        this.Terrain = terrain;
        this.Type = type;
      }
      /// <summary>Links this waypoint to another waypoint, denoting the autopilot can go from <see cref="this"/> to <paramref name="wp"/>. It is not bidirectionnal.</summary>
      /// <param name="wp">Waypoint reachable from this waypoint.</param>
      public void AddWP(APWaypoint wp) {
        if (this.waypointsNames == null) {
          this.waypointsNames = new List<string>();
        }
        this.waypointsNames.Add(wp.Name);
        this.LinkedWps.Add(wp);
      }
      /// <summary>Creates a new waypoint from an ini string, possibly connected to other waypoints</summary>
      /// <param name="ini">ini that contains the waypoints</param>
      /// <param name="section">name of the section containing information about the waypoints</param>
      public APWaypoint(MyIni ini, string section) {
        MyWaypointInfo.TryParse(ini.Get(section, "gps").ToString(), out this.WP);
        Enum.TryParse(ini.Get(section, "type").ToString(), out this.Type);
        Enum.TryParse(ini.Get(section, "terrain").ToString(), out this.Terrain);
        this.waypointsNames = ini.Get(section, "linked-wp").ToString().Split(IniHelper.SEP, StringSplitOptions.RemoveEmptyEntries).ToList();
      }
      /// <summary>Saves the parameters of this waypoint to an ini string</summary>
      /// <param name="ini">Where the waypoint will be saved</param>
      public void Save(MyIni ini) {
        ini.Set(this.Name, "gps", $"GPS:{this.Name}:{this.WP.Coords.X:0.00}:{this.WP.Coords.Y:0.00}:{this.WP.Coords.Z:0.00}:");
        if (this.Terrain != Terrain.Normal) {
          ini.Set(this.Name, "terrain", this.Terrain.ToString());
        }
        if (this.Type != WPType.Path) {
          ini.Set(this.Name, "type", this.Type.ToString());
        }
        if (this.waypointsNames != null) {
          ini.Set(this.Name, "linked-wp", string.Join(",", this.waypointsNames));
        }
      }
      /// <summary>Translates the list of waypoint names from the ini string to a list of actual <see cref="APWaypoint"/></summary>
      /// <param name="network">Network that contains all the waypoints</param>
      /// <param name="logger">Optional logger</param>
      public void Update(WPNetwork network, Action<string> logger) {
        if (this.waypointsNames != null) {
          int notFound = 0;
          foreach (string wpName in this.waypointsNames) {
            APWaypoint wp = network.GetWaypoint(wpName);
            if (wp == null) {
              ++notFound;
            } else {
              this.LinkedWps.Add(wp);
            }
          }
          if (notFound != 0) {
            logger?.Invoke($"Waypoint '{this.Name}' links to {notFound} unknown waypoints");
          }
        }
      }
      /// <summary>For A* search</summary>
      public void ResetAStar() {
        this.DistFromStart = double.MaxValue;
        this.DistToGoal = double.MaxValue;
        this.PrevWP = null;
        this.Visited = false;
        this.aStartDistance = null;
      }
      /// <summary>For A* search</summary>
      public void SetAStarPrev(APWaypoint prev, APWaypoint end, double fromStart) {
        this.DistToGoal = fromStart + this.getAStarDist(end);
        this.PrevWP = prev;
        this.DistFromStart = fromStart;
      }
      /// <summary>For A* search</summary>
      public void Visit() => this.Visited = true;

      double getAStarDist(APWaypoint a) {
        if (this.aStartDistance == null) {
          this.aStartDistance = (this.WP.Coords - a.WP.Coords).Length();
        }
        return this.aStartDistance.Value;
      }
    }
  }
}
