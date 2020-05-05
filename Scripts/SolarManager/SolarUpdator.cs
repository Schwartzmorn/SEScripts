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
    public class SolarUpdator {
      readonly Action<string> logger;
      readonly Program p;

      string keyword;
      List<SolarRotor> solarRotors;

      public SolarUpdator(Program p, Action<string> logger) {
        this.logger = logger;
        this.p = p;
      }

      public void Update(List<SolarRotor> solarRotors, string keyword) {
        this.keyword = keyword;
        this.solarRotors = solarRotors;
        this.p.GridTerminalSystem.GetBlocksOfType(this.tmpPanels, b => b.IsFunctional && (!b.CubeGrid.IsStatic) && b.IsSameConstructAs(p.Me));
        foreach (IMyCubeGrid grid in this.tmpPanels.Select(pan => pan.CubeGrid)) {
          this.tmpSolarGrids.Add(grid);
        }
        foreach (IMyCubeGrid grid in this.tmpSolarGrids) {
          this.tmpSolarMetagridAssociations.Add(grid, grid);
          this.tmpNewGrids.Add(grid);
        }
        while (this.tmpNewGrids.Count != 0) {
          p.GridTerminalSystem.GetBlocksOfType(this.tmpMechBlocks, m => m.IsFunctional && this.tmpNewGrids.Contains(m.TopGrid));
          this.tmpNewGrids.Clear();

          foreach (IMyMechanicalConnectionBlock block in this.tmpMechBlocks) {
            bool isNew = block.CustomName.Contains(this.keyword) && (block as IMyMotorStator) != null
              ? this.addSolarRotor(block as IMyMotorStator)
              : this.addToSolarMetagrid(block);
            if (isNew) {
              this.tmpNewGrids.Add(block.CubeGrid);
            }
          }
          this.tmpMechBlocks.Clear();
        }

        this.checkCycles();

        this.updateRotors();

        this.updateIds();

        this.tmpNewGrids.Clear();
        this.tmpPanels.Clear();
        this.tmpRotorMetagridAssociations.Clear();
        this.tmpSolarMetagridAssociations.Clear();
        this.tmpSolarGrids.Clear();
      }
      // return true if it is a new grid
      bool addToSolarMetagrid(IMyMechanicalConnectionBlock block) {
        IMyCubeGrid topMetaGridId = this.tmpSolarMetagridAssociations[block.TopGrid];
        IMyCubeGrid baseMetaGridId;
        if (this.tmpSolarMetagridAssociations.TryGetValue(block.CubeGrid, out baseMetaGridId)) {
          if (baseMetaGridId != topMetaGridId) {
            this.mergeSolarMetagrids(baseMetaGridId, topMetaGridId);
          }
          return false;
        } else {
          this.tmpSolarMetagridAssociations.Add(block.CubeGrid, topMetaGridId);
          return true;
        }
      }

      bool addSolarRotor(IMyMotorStator rotor) {
        // check if baseMetagrid == topMetagrid
        IMyCubeGrid topMetaGrid = this.tmpSolarMetagridAssociations[rotor.TopGrid];
        IMyCubeGrid baseMetaGridId;
        bool isNew = false;
        if (this.tmpSolarMetagridAssociations.TryGetValue(rotor.CubeGrid, out baseMetaGridId)) {
          if (this.tmpSolarGrids.Contains(baseMetaGridId)) {
            throw new InvalidOperationException($"Solar rotor '{rotor.CustomName}' is on a metagrid with solar panels");
          } else if (baseMetaGridId == topMetaGrid) {
            throw new InvalidOperationException($"Solar rotor '{rotor.CustomName}' has the base and the top in the same metagrid. Add the keyword '{this.keyword}' to the correct rotors.");
          }
        } else {
          // it's a new meta grid
          isNew = true;
          this.tmpSolarMetagridAssociations.Add(rotor.CubeGrid, rotor.CubeGrid);
        }
        this.tmpRotorMetagridAssociations.Add(rotor, topMetaGrid);
        return isNew;
      }

      void mergeSolarMetagrids(IMyCubeGrid metagridA, IMyCubeGrid metagridB) {
        // We use ToList to avoid "Collection was modified" Exception
        foreach (KeyValuePair<IMyCubeGrid, IMyCubeGrid> association in this.tmpSolarMetagridAssociations.Where(kv => kv.Value == metagridB).ToList()) {
          this.tmpSolarMetagridAssociations[association.Key] = metagridA;
        }
        foreach (KeyValuePair<IMyMotorStator, IMyCubeGrid> association in this.tmpRotorMetagridAssociations.Where(kv => kv.Value == metagridB).ToList()) {
          this.tmpRotorMetagridAssociations[association.Key] = metagridA;
        }
      }

      void checkCycles() {
        foreach (KeyValuePair<IMyMotorStator, IMyCubeGrid> rotorAssociation in this.tmpRotorMetagridAssociations) {
          this.checkCycle(rotorAssociation.Key, rotorAssociation.Value, this.tmpRotorCycle);
          this.tmpRotorCycle.Clear();
        }
        this.tmpVisitedRotors.Clear();
      }

      void checkCycle(IMyMotorStator rotor, IMyCubeGrid metagrid, List<IMyMotorStator> rotorCycle) {
        if (rotorCycle.Contains(rotor)) {
          throw new InvalidOperationException($"Cycle detected in solar rotors: {this.formatRotorCycle(rotorCycle, rotor)}");
        }
        if (this.tmpVisitedRotors.Contains(rotor)) {
          return;
        }
        rotorCycle.Add(rotor);
        this.tmpVisitedRotors.Add(rotor);
        foreach (IMyMotorStator r in this.tmpRotorMetagridAssociations.Keys.Where(r => r.CubeGrid == metagrid)) {
          this.checkCycle(r, this.tmpSolarMetagridAssociations[r.CubeGrid], rotorCycle);
        }
        rotorCycle.Pop();
      }

      string formatRotorCycle(List<IMyMotorStator> rotorCycle, IMyMotorStator rotor) {
        int i = rotorCycle.IndexOf(rotor);
        return $"'{string.Join("' => '", rotorCycle.GetRange(i, rotorCycle.Count - i).Select(r => r.CustomName))}'";
      }

      void updateRotors() {
        this.solarRotors.Clear();
        foreach (IMyCubeGrid metagrid in this.tmpRotorMetagridAssociations.Values) {
          this.tmpSolarGridsInMetaGrid.AddRange(this.tmpSolarGrids.Where(grid => this.tmpSolarMetagridAssociations[grid] == metagrid));
          if (this.tmpSolarGridsInMetaGrid.Count != 0) {
            this.tmpControllingRotors.AddRange(this.tmpRotorMetagridAssociations.Where(kv => kv.Value == metagrid).Select(kv => kv.Key));
            var solarPanel = new SolarPanel(this.tmpSolarGridsInMetaGrid, this.tmpPanels);
            var solarRotor = new SolarRotor(this.keyword, this.tmpControllingRotors, solarPanel, this.logger);
            IMyCubeGrid baseMetaGrid = this.tmpSolarMetagridAssociations[this.tmpControllingRotors.First().CubeGrid];
            List<SolarRotor> solarRotors;
            if (this.tmpBaseMetagridSolarRotorAssociation.TryGetValue(baseMetaGrid, out solarRotors)) {
              solarRotors.Add(solarRotor);
            } else {
              this.tmpBaseMetagridSolarRotorAssociation.Add(baseMetaGrid, new List<SolarRotor> { solarRotor });
            }
            this.tmpControllingRotors.Clear();
            this.tmpSolarGridsInMetaGrid.Clear();
          }
        }
        foreach (KeyValuePair<IMyCubeGrid, List<SolarRotor>> kv in this.tmpBaseMetagridSolarRotorAssociation) {
          this.tmpControllingRotors.AddRange(this.tmpRotorMetagridAssociations.Where(kv2 => kv2.Value == kv.Key).Select(kv2 => kv2.Key));
          if (this.tmpControllingRotors.Count == 0) {
            this.solarRotors.AddRange(kv.Value);
          } else {
            this.solarRotors.Add(new SolarRotor(this.keyword, this.tmpControllingRotors, kv.Value, this.logger));
            this.tmpControllingRotors.Clear();
          }
        }
        this.tmpBaseMetagridSolarRotorAssociation.Clear();
      }

      void updateIds() {
        var usedIds = new HashSet<int>(this.solarRotors.Select(r => r.IDNumber));
        int id = 1;
        foreach (SolarRotor rotor in this.solarRotors.Where(r => r.IDNumber == 0)) {
          while (usedIds.Contains(id)) {
            ++id;
          }
          rotor.IDNumber = id;
          usedIds.Add(id);
        }
      }

      // temporary containers for re use during grid gathering
      // blocks whose top is connected to the new grids
      readonly List<IMyMechanicalConnectionBlock> tmpMechBlocks = new List<IMyMechanicalConnectionBlock>();
      // new grids encountered during the last cycle
      readonly HashSet<IMyCubeGrid> tmpNewGrids = new HashSet<IMyCubeGrid>();
      // list of all the solar panels
      readonly List<IMySolarPanel> tmpPanels = new List<IMySolarPanel>();
      // chain of rotors visited during the cycle check
      readonly List<IMyMotorStator> tmpRotorCycle = new List<IMyMotorStator>();
      // Associates a rotor to the metagrid it controls
      readonly Dictionary<IMyMotorStator, IMyCubeGrid> tmpRotorMetagridAssociations = new Dictionary<IMyMotorStator, IMyCubeGrid>();
      // association grid => metagrid
      readonly Dictionary<IMyCubeGrid, IMyCubeGrid> tmpSolarMetagridAssociations = new Dictionary<IMyCubeGrid, IMyCubeGrid>();
      // Metagrids that have solar panels
      readonly HashSet<IMyCubeGrid> tmpSolarGrids = new HashSet<IMyCubeGrid>();
      // visited grids during the cycle checks
      readonly HashSet<IMyMotorStator> tmpVisitedRotors = new HashSet<IMyMotorStator>();

      // temporary containers for re use during rotor update
      // association base metagrid => solar rotors
      readonly Dictionary<IMyCubeGrid, List<SolarRotor>> tmpBaseMetagridSolarRotorAssociation = new Dictionary<IMyCubeGrid, List<SolarRotor>>();
      // holds the list solar rotor controlling a metagrid
      readonly List<IMyMotorStator> tmpControllingRotors = new List<IMyMotorStator>();
      // holds the list of solar grids in a metagrid
      readonly List<IMyCubeGrid> tmpSolarGridsInMetaGrid = new List<IMyCubeGrid>();
    }
  }
}
