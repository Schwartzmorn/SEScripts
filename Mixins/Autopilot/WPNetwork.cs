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
    /// <summary>Class that holds a network of <see cref="APWaypoint"/> to provide some pathfinding</summary>
    public class WPNetwork
    {
      readonly Dictionary<string, APWaypoint> _waypoints = new Dictionary<string, APWaypoint>();
      readonly IMyRemoteControl _remote;
      string _prevData;
      readonly List<string> _tmpSections = new List<string>();
      readonly List<MyWaypointInfo> _tmpWps = new List<MyWaypointInfo>();
      readonly Action<string> _logger;
      readonly SortedList<double, APWaypoint> _aStarQueue = new SortedList<double, APWaypoint>();
      /// <summary>Creates a new network</summary>
      /// <param name="remote">The remote doing the autopilot, and whose position is used as a referential</param>
      /// <param name="logger">Optional logger</param>
      /// <param name="spawner">Used to spawn the update processes</param>
      public WPNetwork(IMyRemoteControl remote, Action<string> logger, IProcessSpawner spawner)
      {
        _logger = logger;
        _remote = remote;
        _updateData();
        spawner.Spawn(p => _updateData(), "wpn-update", period: 100);
      }
      /// <summary>Returns the waypoint with the given name, null if it does not exist</summary>
      /// <param name="name">Name of the waypoint</param>
      /// <returns>The waypoint</returns>
      public APWaypoint GetWaypoint(string name)
      {
        APWaypoint res;
        _waypoints.TryGetValue(name, out res);
        return res;
      }
      /// <summary>Creates a new waypoint from the position of the <see cref="_remote"/> that is reachable from the waypoint named <paramref name="linkedWpN"/></summary>
      /// <param name="name">Name of the new waypoint</param>
      /// <param name="linkedWpN">Name of the waypoint that is biderctionally reachable</param>
      /// <param name="terrain">Terrain type of the new waypoint</param>
      /// <param name="type">Type of the new waypoint</param>
      public void AddLinkedWP(string name, string linkedWpN, Terrain terrain = Terrain.Dangerous, WPType type = WPType.PrecisePath)
      {
        APWaypoint linkedWP = _waypoints[linkedWpN];
        var wp = new APWaypoint(new MyWaypointInfo(name, _remote.GetPosition()), terrain, type);
        linkedWP.AddWP(wp);
        wp.AddWP(linkedWP);
        _waypoints[name] = wp;
        _save();
      }
      /// <summary>Adds a new waypoint or updates the position of an existing waypoint</summary>
      /// <param name="wp">Waypoint to add / update</param>
      public void Add(MyWaypointInfo wp)
      {
        APWaypoint apwp;
        if (_waypoints.TryGetValue(wp.Name, out apwp))
        {
          apwp.WP = wp;
          _logger?.Invoke($"Updated waypoint {wp.Name}");
          _save();
        }
        else
        {
          _waypoints[wp.Name] = new APWaypoint(wp);
          _logger?.Invoke($"Added waypoint {wp.Name}");
          _save();
        }
      }
      /// <summary>Computes the path to go from a position to a waypoint</summary>
      /// <param name="pos">Starting position</param>
      /// <param name="end">Waypoint to reach</param>
      /// <param name="path">will be filled with a list of waypoint representing the path to follow</param>
      public void GetPath(Vector3D pos, APWaypoint end, List<APWaypoint> path)
      {
        path.Clear();

        APWaypoint start = _getClosest(pos);
        if (start == null)
        {
          return;
        }

        _aStarQueue.Clear();
        foreach (APWaypoint wp in _waypoints.Values)
        {
          wp.ResetAStar();
        }

        start.SetAStarPrev(null, end, 0);
        _aStarQueue.Add(start.DistToGoal, start);

        while (_aStarQueue.Count != 0)
        {
          APWaypoint curWP = _aStarQueue.ElementAt(0).Value;

          if (curWP == end)
          {
            APWaypoint cur = end;
            while (cur != null)
            {
              path.Add(cur);
              cur = cur.PrevWP;
            }
          }

          _aStarQueue.RemoveAt(0);
          curWP.Visit();

          foreach (APWaypoint wp in curWP.LinkedWps)
          {
            if (wp.Visited)
            {
              continue;
            }
            double fromStart = curWP.DistFromStart + (curWP.WP.Coords - wp.WP.Coords).Length();
            if (wp.PrevWP == null)
            {
              wp.SetAStarPrev(curWP, end, fromStart);
              _aStarQueue.Add(wp.DistToGoal, wp);
            }
            else if (fromStart >= wp.DistFromStart)
            {
              continue;
            }
            else
            {
              _aStarQueue.Remove(wp.DistToGoal);
              wp.SetAStarPrev(curWP, end, fromStart);
              _aStarQueue.Add(wp.DistToGoal, wp);
            }
          }
        }
        if (path.Count > 1 && path.Last().WP.Name.StartsWith(","))
        {
          path.Last().WP.Coords = (path.Last().WP.Coords + path[path.Count - 2].WP.Coords) / 2;
        }
      }

      APWaypoint _getClosest(Vector3D pos)
      {
        double maxDist = double.MaxValue;
        APWaypoint cA = null, cB = null;
        foreach (APWaypoint wp in _waypoints.Values)
        {
          wp.ResetAStar();
        }
        foreach (APWaypoint wpA in _waypoints.Values)
        {
          wpA.Visit();
          foreach (APWaypoint wpB in wpA.LinkedWps.Where(w => !w.Visited))
          {
            MyTuple<double, APWaypoint> dist = _getDist(pos, wpA, wpB);
            if (dist.Item1 < maxDist)
            {
              if (dist.Item2 == null)
              {
                cA = wpA;
                cB = wpB;
              }
              else
              {
                cA = dist.Item2;
                cB = null;
              }
              maxDist = dist.Item1;
            }
          }
        }
        if (cB == null)
        {
          return cA;
        }
        else
        {
          Vector3D b = cB.WP.Coords;
          var ba = Vector3D.Normalize(cA.WP.Coords - b);
          var res = new APWaypoint(new MyWaypointInfo(",TMPSTART", b + ((pos - b).Dot(ba) * ba)));
          res.AddWP(cA);
          res.AddWP(cB);
          return res;
        }
      }

      void _updateData()
      {
        bool changed = false;
        if (!ReferenceEquals(_prevData, _remote.CustomData))
        {
          _prevData = _remote.CustomData;
          var ini = new MyIni();
          ini.Parse(_remote.CustomData);
          ini.GetSections(_tmpSections);
          _waypoints.Clear();
          _tmpSections.ForEach(s => _waypoints.Add(s, new APWaypoint(ini, s)));
          changed = true;
        }
        _remote.GetWaypointInfo(_tmpWps);
        if (_tmpWps.Count > 0)
        {
          foreach (MyWaypointInfo wp in _tmpWps)
          {
            APWaypoint curWP;
            if (_waypoints.TryGetValue(wp.Name, out curWP))
            {
              curWP.WP = wp;
            }
            else
            {
              _waypoints.Add(wp.Name, new APWaypoint(wp));
            }
          }
          _save();
          _remote.ClearWaypoints();
        }
        if (changed)
        {
          foreach (APWaypoint wp in _waypoints.Values)
          {
            wp.Update(this, _logger);
          }
        }
      }

      void _save()
      {
        var ini = new MyIni();
        foreach (APWaypoint wp in _waypoints.Values)
        {
          wp.Save(ini);
        }
        _remote.CustomData = ini.ToString();
        _prevData = _remote.CustomData;
      }

      MyTuple<double, APWaypoint> _getDist(Vector3D pos, APWaypoint a, APWaypoint b)
      {
        Vector3D ab = b.WP.Coords - a.WP.Coords;
        Vector3D ap = pos - a.WP.Coords;
        if (ap.Dot(ab) <= 0)
        {
          return MyTuple.Create(ap.LengthSquared(), a);
        }
        Vector3D bp = pos - b.WP.Coords;
        return bp.Dot(ab) >= 0
          ? MyTuple.Create(bp.LengthSquared(), b)
          : MyTuple.Create(ab.Cross(ap).LengthSquared() / ab.LengthSquared(), null as APWaypoint);
      }
    }
  }
}
