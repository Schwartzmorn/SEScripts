using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public class ContainerManager : LazyFilter
    {
      readonly Dictionary<long, Container> _containers = new Dictionary<long, Container>();

      readonly List<IMyCargoContainer> _tmpList = new List<IMyCargoContainer>();

      readonly List<Container> _tmpSortedList = new List<Container>();

      readonly Action<string> _logger;

      readonly GridManager _gridManager;

      public ContainerManager(IMyGridTerminalSystem gts, GridManager gridManager, IProcessSpawner spawner, Action<string> logger)
      {
        _logger = logger;
        _gridManager = gridManager;
        spawner.Spawn(p => Scan(gts), "container-scanner", period: 100);
        Scan(gts);
      }

      public void Scan(IMyGridTerminalSystem GTS)
      {
        var previousCount = _containers.Count;
        GTS.GetBlocksOfType(_tmpList, cont => cont.GetInventory() != null && _gridManager.Manages(cont.CubeGrid));

        foreach (var c in _tmpList)
        {
          Container cont;
          if (!_containers.TryGetValue(c.EntityId, out cont))
          {
            cont = new Container(c);
            _containers[c.EntityId] = cont;
          }
          cont.ParseIni();
        }

        if (previousCount != _containers.Count)
        {
          _log($"Found {_containers.Count} containers");
        }
      }

      public List<Container> GetSortedCandidateContainers(MyInventoryItem item, int minAff)
      {
        FilterLazily();
        _tmpSortedList.Clear();
        _tmpSortedList.AddRange(_containers.Values.Where(c => c.GetIntrinsicAffinity(item.Type) > minAff));
        _tmpSortedList.Sort((a, b) => Container.CompareTo(a, b, item.Type));
        return _tmpSortedList;
      }

      public IReadOnlyCollection<Container> GetContainers()
      {
        FilterLazily();
        return _containers.Values;
      }

      protected override void Filter()
      {
        foreach (var c in _containers.Where(c => c.Value?.GetInventory() == null || !_gridManager.Manages(c.Value.Cargo.CubeGrid)).ToList())
        {
          _containers.Remove(c.Key);
        }
      }

      void _log(string s) => _logger?.Invoke("CM: " + s);
    }

  }
}
