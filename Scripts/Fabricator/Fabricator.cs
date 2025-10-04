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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class Fabricator {
      const string SECTION = "fabricator";

      readonly FabricatorDisplay display;
      readonly InventoryManager inventoryManager;
      readonly Projector projector;
      readonly Welder welder;
      readonly Action<string> logger;
      readonly Process mainProcess;
      readonly string name;

      public Fabricator(MyIni ini, IMyGridTerminalSystem gts, ISaveManager manager, CommandLine command, Action<string> logger) {
        this.logger = logger;
        this.name = ini.GetThrow(SECTION, "name").ToString();
        this.mainProcess = manager.Spawn(null, "fabricator-process", period: 100);
        this.projector = new Projector(
            AssertNonNull(gts.GetBlockWithName(this.name + " Projector Door") as IMyDoor, $"Could not find '{this.name + " Projector Door"}'"),
            AssertNonNull(gts.GetBlockWithName(this.name + " Projector Piston") as IMyPistonBase, $"Could not find '{this.name + " Projector Piston"}'"),
            AssertNonNull(gts.GetBlockWithName(this.name + " Projector") as IMyProjector, $"Could not find '{this.name + " Projector Projector"}'"),
            this.mainProcess,
            this.log);

        var pistons = new List<IMyPistonBase>();
        gts.GetBlocksOfType(pistons, p => p.CustomName == this.name + " Piston");
        var welders = new List<IMyShipWelder>();
        gts.GetBlocksOfType(welders, w => w.CustomName == this.name + " Welder");
        var lights = new List<IMyLightingBlock>();
        gts.GetBlocksOfType(lights, l => l.CustomName == this.name + " Light");
        this.welder = new Welder(
            AssertNonEmpty(pistons, $"Could not find any '{this.name + " Piston"}'"),
            AssertNonNull(gts.GetBlockWithName(this.name + " Sensor") as IMySensorBlock, $"Could not find '{this.name + " Sensor"}'"),
            AssertNonEmpty(welders, $"Could not find any '{this.name + " Welder"}'"),
            AssertNonEmpty(lights, $"Could not find any '{this.name + " Light"}'"),
            ini.Get(SECTION, "position-multiplier").ToInt32(1),
            this.mainProcess);

        this.inventoryManager = new InventoryManager(this.mainProcess, c => this.containersGetter(c, gts));
        this.display = new FabricatorDisplay(
            AssertNonNull(gts.GetBlockWithName(this.name + " Status Display") as IMyTextPanel, $"Could not find '{this.name + " Status Display"}'"),
            AssertNonNull(gts.GetBlockWithName(this.name + " Mandatory Display") as IMyTextPanel, $"Could not find '{this.name + " Mandatory Display"}'"),
            AssertNonNull(gts.GetBlockWithName(this.name + " Other Display") as IMyTextPanel, $"Could not find '{this.name + " Other Display"}'"),
            this.name,
            this.projector,
            this.welder,
            this.inventoryManager,
            this.mainProcess
        );

        command.RegisterCommand(new Command("fab-deploy", Command.Wrap(this.deploy), "Deploys the blueprint dispenser"));
        command.RegisterCommand(new Command("fab-retract", Command.Wrap(this.retract), "Deploys the blueprint dispenser"));
        command.RegisterCommand(new Command("fab-step", Command.Wrap((Process p) => this.step(gts)), "Activates the welder"));
      }

      void deploy() {
        this.projector.Deploy();
        this.welder.Deploy();
      }

      void retract() {
        this.projector.Retract();
        this.welder.Retract();
      }

      void step(IMyGridTerminalSystem gts) {
        this.projector.AttachEverything(gts);
        WelderStatus welderStatus = this.welder.GetStatus();
        if (welderStatus != WelderStatus.Deployed) {
          this.log("Welder is not ready");
          return;
        }
        ProjectorStatus projStatus = this.projector.GetStatus();
        if (projStatus != ProjectorStatus.Projecting) {
          this.log("Projector is not ready");
          return;
        }
        InventoryStatus invStatus = this.inventoryManager.GetStatus();
        if (invStatus == InventoryStatus.NotReady) {
          this.log("Inventory is not ready");
          return;
        }
        this.welder.Step();
      }

      void containersGetter(List<IMyCargoContainer> containers, IMyGridTerminalSystem gts) {
        gts.GetBlocksOfType(containers, c => c.CubeGrid.IsStatic);
      }

      void log(string s, string source = "fab")  {
        this.display.LastMessage = s;
        this.logger?.Invoke($"{source}: {s}");
      }
    }
  }
}
