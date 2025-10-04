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

namespace IngameScript
{
  partial class Program
  {
    public class SolarUpdator
    {
      readonly Action<string> _logger;
      readonly Program _p;

      string _keyword;
      List<SolarRotor> _solarRotors;

      public SolarUpdator(Program p, Action<string> logger)
      {
        _logger = logger;
        _p = p;
      }

      public void Update(List<SolarRotor> solarRotors, string keyword)
      {
        _keyword = keyword;
        _solarRotors = solarRotors;
        _p.GridTerminalSystem.GetBlocksOfType(_tmpPanels, b => b.IsFunctional && (!b.CubeGrid.IsStatic) && b.IsSameConstructAs(_p.Me));
        foreach (IMyCubeGrid grid in _tmpPanels.Select(pan => pan.CubeGrid))
        {
          _tmpSolarGrids.Add(grid);
        }
        foreach (IMyCubeGrid grid in _tmpSolarGrids)
        {
          _tmpSolarMetagridAssociations.Add(grid, grid);
          _tmpNewGrids.Add(grid);
        }
        while (_tmpNewGrids.Count != 0)
        {
          _p.GridTerminalSystem.GetBlocksOfType(_tmpMechBlocks, m => m.IsFunctional && _tmpNewGrids.Contains(m.TopGrid));
          _tmpNewGrids.Clear();

          foreach (IMyMechanicalConnectionBlock block in _tmpMechBlocks)
          {
            bool isNew = block.CustomName.Contains(_keyword) && (block as IMyMotorStator) != null
              ? _addSolarRotor(block as IMyMotorStator)
              : _addToSolarMetagrid(block);
            if (isNew)
            {
              _tmpNewGrids.Add(block.CubeGrid);
            }
          }
          _tmpMechBlocks.Clear();
        }

        _checkCycles();

        _updateRotors();

        _updateIds();

        _tmpNewGrids.Clear();
        _tmpPanels.Clear();
        _tmpRotorMetagridAssociations.Clear();
        _tmpSolarMetagridAssociations.Clear();
        _tmpSolarGrids.Clear();
      }
      // return true if it is a new grid
      bool _addToSolarMetagrid(IMyMechanicalConnectionBlock block)
      {
        IMyCubeGrid topMetaGridId = _tmpSolarMetagridAssociations[block.TopGrid];
        IMyCubeGrid baseMetaGridId;
        if (_tmpSolarMetagridAssociations.TryGetValue(block.CubeGrid, out baseMetaGridId))
        {
          if (baseMetaGridId != topMetaGridId)
          {
            _mergeSolarMetagrids(baseMetaGridId, topMetaGridId);
          }
          return false;
        }
        else
        {
          _tmpSolarMetagridAssociations.Add(block.CubeGrid, topMetaGridId);
          return true;
        }
      }

      bool _addSolarRotor(IMyMotorStator rotor)
      {
        // check if baseMetagrid == topMetagrid
        IMyCubeGrid topMetaGrid = _tmpSolarMetagridAssociations[rotor.TopGrid];
        IMyCubeGrid baseMetaGridId;
        bool isNew = false;
        if (_tmpSolarMetagridAssociations.TryGetValue(rotor.CubeGrid, out baseMetaGridId))
        {
          if (_tmpSolarGrids.Contains(baseMetaGridId))
          {
            throw new InvalidOperationException($"Solar rotor '{rotor.CustomName}' is on a metagrid with solar panels");
          }
          else if (baseMetaGridId == topMetaGrid)
          {
            throw new InvalidOperationException($"Solar rotor '{rotor.CustomName}' has the base and the top in the same metagrid. Add the keyword '{_keyword}' to the correct rotors.");
          }
        }
        else
        {
          // it's a new meta grid
          isNew = true;
          _tmpSolarMetagridAssociations.Add(rotor.CubeGrid, rotor.CubeGrid);
        }
        _tmpRotorMetagridAssociations.Add(rotor, topMetaGrid);
        return isNew;
      }

      void _mergeSolarMetagrids(IMyCubeGrid metagridA, IMyCubeGrid metagridB)
      {
        // We use ToList to avoid "Collection was modified" Exception
        foreach (KeyValuePair<IMyCubeGrid, IMyCubeGrid> association in _tmpSolarMetagridAssociations.Where(kv => kv.Value == metagridB).ToList())
        {
          _tmpSolarMetagridAssociations[association.Key] = metagridA;
        }
        foreach (KeyValuePair<IMyMotorStator, IMyCubeGrid> association in _tmpRotorMetagridAssociations.Where(kv => kv.Value == metagridB).ToList())
        {
          _tmpRotorMetagridAssociations[association.Key] = metagridA;
        }
      }

      void _checkCycles()
      {
        foreach (KeyValuePair<IMyMotorStator, IMyCubeGrid> rotorAssociation in _tmpRotorMetagridAssociations)
        {
          _checkCycle(rotorAssociation.Key, rotorAssociation.Value, _tmpRotorCycle);
          _tmpRotorCycle.Clear();
        }
        _tmpVisitedRotors.Clear();
      }

      void _checkCycle(IMyMotorStator rotor, IMyCubeGrid metagrid, List<IMyMotorStator> rotorCycle)
      {
        if (rotorCycle.Contains(rotor))
        {
          throw new InvalidOperationException($"Cycle detected in solar rotors: {_formatRotorCycle(rotorCycle, rotor)}");
        }
        if (_tmpVisitedRotors.Contains(rotor))
        {
          return;
        }
        rotorCycle.Add(rotor);
        _tmpVisitedRotors.Add(rotor);
        foreach (IMyMotorStator r in _tmpRotorMetagridAssociations.Keys.Where(r => r.CubeGrid == metagrid))
        {
          _checkCycle(r, _tmpSolarMetagridAssociations[r.CubeGrid], rotorCycle);
        }
        rotorCycle.Pop();
      }

      string _formatRotorCycle(List<IMyMotorStator> rotorCycle, IMyMotorStator rotor)
      {
        int i = rotorCycle.IndexOf(rotor);
        return $"'{string.Join("' => '", rotorCycle.GetRange(i, rotorCycle.Count - i).Select(r => r.CustomName))}'";
      }

      void _updateRotors()
      {
        _solarRotors.Clear();
        foreach (IMyCubeGrid metagrid in _tmpRotorMetagridAssociations.Values)
        {
          _tmpSolarGridsInMetaGrid.AddRange(_tmpSolarGrids.Where(grid => _tmpSolarMetagridAssociations[grid] == metagrid));
          if (_tmpSolarGridsInMetaGrid.Count != 0)
          {
            _tmpControllingRotors.AddRange(_tmpRotorMetagridAssociations.Where(kv => kv.Value == metagrid).Select(kv => kv.Key));
            var solarPanel = new SolarPanel(_tmpSolarGridsInMetaGrid, _tmpPanels);
            var solarRotor = new SolarRotor(_keyword, _tmpControllingRotors, solarPanel, _logger);
            IMyCubeGrid baseMetaGrid = _tmpSolarMetagridAssociations[_tmpControllingRotors.First().CubeGrid];
            List<SolarRotor> solarRotors;
            if (_tmpBaseMetagridSolarRotorAssociation.TryGetValue(baseMetaGrid, out solarRotors))
            {
              solarRotors.Add(solarRotor);
            }
            else
            {
              _tmpBaseMetagridSolarRotorAssociation.Add(baseMetaGrid, new List<SolarRotor> { solarRotor });
            }
            _tmpControllingRotors.Clear();
            _tmpSolarGridsInMetaGrid.Clear();
          }
        }
        foreach (KeyValuePair<IMyCubeGrid, List<SolarRotor>> kv in _tmpBaseMetagridSolarRotorAssociation)
        {
          _tmpControllingRotors.AddRange(_tmpRotorMetagridAssociations.Where(kv2 => kv2.Value == kv.Key).Select(kv2 => kv2.Key));
          if (_tmpControllingRotors.Count == 0)
          {
            _solarRotors.AddRange(kv.Value);
          }
          else
          {
            _solarRotors.Add(new SolarRotor(_keyword, _tmpControllingRotors, kv.Value, _logger));
            _tmpControllingRotors.Clear();
          }
        }
        _tmpBaseMetagridSolarRotorAssociation.Clear();
      }

      void _updateIds()
      {
        var usedIds = new HashSet<int>(_solarRotors.Select(r => r.IDNumber));
        int id = 1;
        foreach (SolarRotor rotor in _solarRotors.Where(r => r.IDNumber == 0))
        {
          while (usedIds.Contains(id))
          {
            ++id;
          }
          rotor.IDNumber = id;
          usedIds.Add(id);
        }
      }

      // temporary containers for re use during grid gathering
      // blocks whose top is connected to the new grids
      readonly List<IMyMechanicalConnectionBlock> _tmpMechBlocks = new List<IMyMechanicalConnectionBlock>();
      // new grids encountered during the last cycle
      readonly HashSet<IMyCubeGrid> _tmpNewGrids = new HashSet<IMyCubeGrid>();
      // list of all the solar panels
      readonly List<IMySolarPanel> _tmpPanels = new List<IMySolarPanel>();
      // chain of rotors visited during the cycle check
      readonly List<IMyMotorStator> _tmpRotorCycle = new List<IMyMotorStator>();
      // Associates a rotor to the metagrid it controls
      readonly Dictionary<IMyMotorStator, IMyCubeGrid> _tmpRotorMetagridAssociations = new Dictionary<IMyMotorStator, IMyCubeGrid>();
      // association grid => metagrid
      readonly Dictionary<IMyCubeGrid, IMyCubeGrid> _tmpSolarMetagridAssociations = new Dictionary<IMyCubeGrid, IMyCubeGrid>();
      // Metagrids that have solar panels
      readonly HashSet<IMyCubeGrid> _tmpSolarGrids = new HashSet<IMyCubeGrid>();
      // visited grids during the cycle checks
      readonly HashSet<IMyMotorStator> _tmpVisitedRotors = new HashSet<IMyMotorStator>();

      // temporary containers for re use during rotor update
      // association base metagrid => solar rotors
      readonly Dictionary<IMyCubeGrid, List<SolarRotor>> _tmpBaseMetagridSolarRotorAssociation = new Dictionary<IMyCubeGrid, List<SolarRotor>>();
      // holds the list solar rotor controlling a metagrid
      readonly List<IMyMotorStator> _tmpControllingRotors = new List<IMyMotorStator>();
      // holds the list of solar grids in a metagrid
      readonly List<IMyCubeGrid> _tmpSolarGridsInMetaGrid = new List<IMyCubeGrid>();
    }
  }
}
