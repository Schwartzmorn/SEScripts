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
  partial class Program : MyGridProgram {
    static Action<string> ECHO;
    static IMyGridProgramRuntimeInfo RUNTIME;
    static DateTime START_TIME;
    static char[] SPLIT_OPTIONS_CHAR = new char[] { '\n' };
    static char[] SPLIT_VALUES_CHAR = new char[] { ',' };

    private IEnumerator<bool> _initialization;
    private AssemblerManager _assemblerManager;
    private ContainerManager _containerManager;
    private GridManager _gridManager;

    public Program() {
      _initialization = Init();
    }

    public void Save() {
    }

    private IEnumerator<bool> Init() {
      EchoStat("GridManager creation start");
      _gridManager = new GridManager(Me, GridTerminalSystem);
      EchoStat("GridManager creation end");
      yield return false;
      EchoStat("Assemblers creation start");
      _assemblerManager = new AssemblerManager(GridTerminalSystem, _gridManager);
      EchoStat("Assemblers creation end");
      yield return false;
      EchoStat("ContainerManager creation start");
      _containerManager = new ContainerManager(GridTerminalSystem, _gridManager);
      EchoStat("ContainerManager creation end");
      yield return false;
      EchoStat("Inventories creation start");
      ScanMiscInventories(GridTerminalSystem, _gridManager, true);
      EchoStat("Inventories creation end");
      yield return false;
      EchoStat("Refineries creation start");
      ScanRefineries(GridTerminalSystem, _gridManager, true);
      EchoStat("Refineries creation end");
      yield return true;
    }

    /*public void Groom(IGroomableManager groomable) {
      foreach(IMyInventory inv in groomable.GetGroomableInventories()) {
        int prevCount = inv.GetItems().Count;
        int idx = prevCount - 1;
        while (idx >= 0) {
          foreach(Container cont in _containersManager.GetSortedContainers(inv.GetItems()[idx])) {
            inv.TransferItemTo(cont.GetInventory(), idx);
            if (inv.GetItems().Count != prevCount) {
              // transfer successful
              prevCount = inv.GetItems().Count;
              break;
            }
          }
          --idx;
        }
      }
    }

    public void GroomContainers() {
      foreach (Container cont in _containersManager.GetContainers()) {
        IMyInventory inv = cont.GetInventory();
        int prevCount = inv.GetItems().Count;
        int idx = prevCount - 1;
        while (idx >= 0) {
          IMyInventoryItem item = inv.GetItems()[idx];
          int affinity = cont.GetAffinity(item);
          foreach (Container cont2 in _containersManager.GetSortedContainers(cont.GetInventory().GetItems()[idx])) {
            int targetAffinity = cont2.GetAffinity(item);
            if (targetAffinity <= affinity) {
              // we only transfer items to a container with a strictly higher affinity
              break;
            }
            cont.GetInventory().TransferItemTo(cont2.GetInventory(), idx);
            if (inv.GetItems().Count != prevCount) {
              // transfer successful
              prevCount = inv.GetItems().Count;
              break;
            }
          }
          --idx;
        }
      }
    }*/

    public void Act() {
    }

    static void EchoStat(string message) => ECHO((DateTime.Now - START_TIME).TotalMilliseconds.ToString("#,000.00") + " - " + RUNTIME.CurrentInstructionCount + '\n' + message + '\n');

    public void Main(string argument, UpdateType updateSource) {
      ECHO = Echo;
      RUNTIME = Runtime;
      START_TIME = DateTime.Now;
      try {
        if (_initialization != null) {
          Echo("Initializing");
          if (!_initialization.MoveNext()) {
            _initialization.Dispose();
            _initialization = null;
          }
        } else {
          Echo("Acting");
          Act();
        }
      } catch (Exception e) {
        // Dump the exception content to the 
        Echo("An error occurred during script execution.");
        Echo($"Exception: {e}\n---");

        // Rethrow the exception to make the programmable block halt execution properly
        throw;
      }
    }
  }
}