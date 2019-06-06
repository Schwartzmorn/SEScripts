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
  public class WPNetwork {
    readonly Dictionary<string, APWaypoint> _wps = new Dictionary<string, APWaypoint>();

    IMyRemoteControl _remote;
    string _prevData;
    readonly List<string> _tmpSections = new List<string>();
    readonly List<MyWaypointInfo> _tmpWps = new List<MyWaypointInfo>();

    SortedList<double, APWaypoint> _asQ = new SortedList<double, APWaypoint>();

    public WPNetwork(IMyRemoteControl remote) {
      _remote = remote;
      _updData();
      Schedule(new ScheduledAction(() => _updData(), 100, name: "wpn-update"));
    }

    public APWaypoint GetWP(string name) {
      APWaypoint res = null;
      _wps.TryGetValue(name, out res);
      return res;
    }

    public void AddLinkedWP(string name, string linkedWpN, Terrain terrain = Terrain.Dangerous, WPType type = WPType.PrecisePath) {
      var linkedWP = _wps[linkedWpN];
      var wp = new APWaypoint(new MyWaypointInfo(name, _remote.GetPosition()), terrain, type);
      linkedWP.AddWP(wp);
      wp.AddWP(linkedWP);
      _wps[name] = wp;
      _save();
    }

    public void Add(MyWaypointInfo wp) {
      APWaypoint apwp;
      if (_wps.TryGetValue(wp.Name, out apwp)) {
        apwp.WP = wp;
        Log($"Updated waypoint {wp.Name}");
        _save();
      } else {
        _wps[wp.Name] = new APWaypoint(wp);
        Log($"Added waypoint {wp.Name}");
        _save();
      }
    }

    public void GetPath(Vector3D pos, APWaypoint end, List<APWaypoint> path) {
      path.Clear();

      var start = _getClosest(pos);
      if (start == null)
        return;

      _asQ.Clear();
      foreach (var wp in _wps.Values)
        wp.ResetAS();

      start.SetASPrev(null, end, 0);
      _asQ.Add(start.DistToGoal, start);

      while (_asQ.Count != 0 ) {
        var curWP = _asQ.ElementAt(0).Value;

        if (curWP == end) {
          APWaypoint cur = end;
          while (cur != null) {
            path.Add(cur);
            cur = cur.PrevWP;
          }
        }

        _asQ.RemoveAt(0);
        curWP.Visit();

        foreach(var wp in curWP.LinkedWps) {
          if (wp.Visited) continue;
          double fromStart = curWP.DistFromStart + (curWP.WP.Coords - wp.WP.Coords).Length();
          if (wp.PrevWP == null) {
            wp.SetASPrev(curWP, end, fromStart);
            _asQ.Add(wp.DistToGoal, wp);
          } else if (fromStart >= wp.DistFromStart) continue;
          else {
            _asQ.Remove(wp.DistToGoal);
            wp.SetASPrev(curWP, end, fromStart);
            _asQ.Add(wp.DistToGoal, wp);
          }
        }
      }
      if (path.Count > 1 && path.Last().WP.Name.StartsWith(","))
        path.Last().WP.Coords = (path.Last().WP.Coords + path[path.Count - 2].WP.Coords) / 2;
    }

    APWaypoint _getClosest(Vector3D pos) {
      double maxDist = double.MaxValue;
      APWaypoint cA = null, cB = null;
      foreach(var wp in _wps.Values)
        wp.ResetAS();
      foreach(var wpA in _wps.Values) {
        wpA.Visit();
        foreach(var wpB in wpA.LinkedWps.Where(w => !w.Visited)) {
          var dist = _getDist(pos, wpA, wpB);
          if(dist.Item1 < maxDist) {
            if(dist.Item2 == null) {
              cA = wpA;
              cB = wpB;
            } else {
              cA = dist.Item2;
              cB = null;
            }
            maxDist = dist.Item1;
          }
        }
      }
      if(cB == null)
        return cA;
      else {
        var b = cB.WP.Coords;
        var ba = Vector3D.Normalize(cA.WP.Coords - b);
        var res = new APWaypoint(new MyWaypointInfo(",TMPSTART", b + ((pos - b).Dot(ba) * ba)));
        res.AddWP(cA);
        res.AddWP(cB);
        return res;
      }
    }

    private void _updData() {
      bool changed = false;
      if(!ReferenceEquals(_prevData, _remote.CustomData)) {
        _prevData = _remote.CustomData;
        var ini = new Ini(_remote);
        ini.GetSections(_tmpSections);
        _wps.Clear();
        _tmpSections.ForEach(s => _wps.Add(s, new APWaypoint(ini, s)));
        changed = true;
      }
      _remote.GetWaypointInfo(_tmpWps);
      if (_tmpWps.Count > 0) {
        foreach(var wp in _tmpWps) {
          APWaypoint curWP;
          if(_wps.TryGetValue(wp.Name, out curWP)) {
            curWP.WP = wp;
          } else {
            _wps.Add(wp.Name, new APWaypoint(wp));
          }
        }
        _save();
        _remote.ClearWaypoints();
      }
      if (changed)
        foreach(var wp in _wps.Values)
          wp.Update(this);
    }

    void _save() {
      var ini = new MyIni();
      foreach(var wp in _wps.Values) {
        wp.Save(ini);
      }
      _remote.CustomData = ini.ToString();
      _prevData = _remote.CustomData;
    }

    MyTuple<double, APWaypoint> _getDist(Vector3D pos, APWaypoint a, APWaypoint b) {
      var ab = b.WP.Coords - a.WP.Coords;
      var ap = pos - a.WP.Coords;
      if (ap.Dot(ab) <= 0)
        return MyTuple.Create(ap.LengthSquared(), a);
      var bp = pos - b.WP.Coords;
      return bp.Dot(ab) >= 0
        ? MyTuple.Create(bp.LengthSquared(), b)
        : MyTuple.Create(ab.Cross(ap).LengthSquared() / ab.LengthSquared(), null as APWaypoint);
    }
  }
}
}
