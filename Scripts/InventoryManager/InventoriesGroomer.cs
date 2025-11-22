using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public class InventoriesGroomer
    {
      int _outputCounter = 0;
      readonly List<IOutputInventoryCollection> _outputInventories;
      readonly Action<string> _logger;

      public InventoriesGroomer(ContainerManager contManager, AssemblerManager assemblerManager,
          MiscInventoryManager miscInventoryManager, RefineryManager refineryManager, IProcessSpawner spawner, Action<string> logger)
      {
        _logger = logger;
        _outputInventories = new List<IOutputInventoryCollection> { assemblerManager, miscInventoryManager, refineryManager };
        spawner.Spawn(p => _groomContainers(contManager), "container-groomer", period: 50);
        spawner.Spawn(p => _groomOutputInventories(contManager), "producer-groomer", period: 50);
        spawner.Spawn(p => _groomContainerInventories(contManager), "inventories-groomer", period: 50);
      }

      void _groomOutputInventories(ContainerManager contManager)
      {
        var collection = _outputInventories[_outputCounter];
        int numberOfFailedGrooms = 0;
        int numberOfSuccessfulGrooms = 0;

        foreach (var fromInv in collection.GetOutputInventories())
        {
          int iFrom = fromInv.ItemCount - 1;
          while (iFrom >= 0)
          {
            MyInventoryItem item = fromInv.GetItemAt(iFrom).Value;
            int prevCount = fromInv.ItemCount;
            int prevFrom = iFrom;
            foreach (var toCont in contManager.GetSortedCandidateContainers(item, 0))
            {
              toCont.GetInventory().TransferItemFrom(fromInv, iFrom);
              if (prevCount != fromInv.ItemCount)
              {
                ++numberOfSuccessfulGrooms;
                --iFrom;
                break;
              }
            }
            if (prevFrom == iFrom)
            {
              ++numberOfFailedGrooms;
              --iFrom;
            }
          }
        }

        ++_outputCounter;
        if (_outputCounter >= _outputInventories.Count)
        {
          _outputCounter = 0;
        }
        if (numberOfFailedGrooms > 0)
        {
          _log($"Failed to move {numberOfFailedGrooms} item(s) from {collection.Name}");
        }
        if (numberOfSuccessfulGrooms > 0)
        {
          _log($"Moved {numberOfSuccessfulGrooms} item(s) from {collection.Name}");
        }
      }

      void _groomContainers(ContainerManager contManager)
      {
        int numberOfFailedGrooms = 0;
        int numberOfSuccessfulGrooms = 0;
        foreach (var fromCont in contManager.GetContainers())
        {
          int iFrom = fromCont.GetInventory().ItemCount - 1;
          while (iFrom >= 0)
          {
            MyInventoryItem item = fromCont.GetInventory().GetItemAt(iFrom).Value;
            int fromAff = fromCont.GetAffinity(item.Type);
            int prevCount = fromCont.GetInventory().ItemCount;
            int prevFrom = iFrom;
            bool shouldMove = false;
            foreach (var toCont in contManager.GetSortedCandidateContainers(item, fromAff))
            {
              if (toCont.GetAffinity(item.Type) > fromAff)
              {
                shouldMove = true;
                fromCont.GetInventory().TransferItemTo(toCont.GetInventory(), item);
                if (prevCount != fromCont.GetInventory().ItemCount)
                {
                  --iFrom;
                  ++numberOfSuccessfulGrooms;
                  break;
                }
              }
              else
              {
                if (shouldMove)
                {
                  ++numberOfFailedGrooms;
                }
                --iFrom;
                break;
              }
            }
            if (prevFrom == iFrom)
            {
              --iFrom;
            }
          }
        }
        if (numberOfFailedGrooms > 0)
        {
          _log($"Failed to move {numberOfFailedGrooms} item(s)");
        }
      }

      void _groomContainerInventories(ContainerManager contManager)
      {
        var items = new Dictionary<MyItemType, int>();
        foreach (var cont in contManager.GetContainers())
        {
          items.Clear();
          var inventory = cont.GetInventory();

          int index = 0;
          while (index < inventory.ItemCount)
          {
            var itemType = inventory.GetItemAt(index).Value.Type;
            int previousIndex;
            if (items.TryGetValue(itemType, out previousIndex))
            {
              inventory.TransferItemTo(inventory, index, previousIndex);
            }
            else
            {
              items.Add(itemType, index);
              index++;
            }
          }
        }
      }

      void _log(string s) => _logger?.Invoke("IG: " + s);
    }
  }
}
