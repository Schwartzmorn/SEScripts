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
    public class SolarManager
    {
      const string SECTION = "solar-manager";
      const float DAY_RATIO = 0.8f;
      const float NIGHT_RATIO = 0.7f;

      public float CurrentOutput => _solarRotors.Sum(r => r.CurrentOutput);
      public float MaxOutput => _solarRotors.Sum(r => r.MaxOutput);
      public int PanelCount => _solarRotors.Sum(r => r.PanelCount);
      public int RotorCount => _solarRotors.Count;

      readonly string _keyword;
      readonly Action<string> _logger;
      readonly List<SolarRotor> _solarRotors = new List<SolarRotor>();
      readonly SolarUpdator _updator;
      bool _nightMode;
      int _tickCount = 0;

      public SolarManager(Program p, CommandLine command, ISaveManager manager, Action<string> logger)
      {
        _logger = logger;
        var ini = new MyIni();
        ini.TryParse(p.Me.CustomData);
        _keyword = ini.GetThrow(SECTION, "keyword").ToString("Solar Rotor");
        _nightMode = ini.Get(SECTION, "night-mode").ToBoolean();
        Process main = manager.Spawn(process => _main(), "solar-manager", period: 50);
        _updator = new SolarUpdator(p, logger);
        _update();
        main.Spawn(process => _update(), "solar-manager-update", period: 10000);
        manager.AddOnSave(_save);
        command.RegisterCommand(new Command("solar-adjust", Command.Wrap(_adjust), @"Adjusts the position of a rotor.
first argument is the offset in degree
second argument is the id of the rotor
third (optional) is the id of the auxilliary rotor", minArgs: 2, maxArgs: 3));
        command.RegisterCommand(new Command("solar-track", Command.Wrap(_track), "forces an idle rotor to start tracking", nArgs: 1));
      }

      void _adjust(ArgumentsWrapper args)
      {
        float offset = float.Parse(args[0]);
        int rotorId = int.Parse(args[1]);
        int rotorAuxId = (args.RemaingCount == 3) ? int.Parse(args[2]) : 0;
        _solarRotors.FirstOrDefault(r => r.IDNumber == rotorId)?.Adjust(offset, rotorAuxId);
      }

      void _track(string arg)
      {
        int rotorId = int.Parse(arg);
        _solarRotors.FirstOrDefault(r => r.IDNumber == rotorId)?.Track();
      }

      void _save(MyIni ini)
      {
        ini.Set(SECTION, "night-mode", _nightMode);
        _saveRotors();
      }

      void _main()
      {
        ++_tickCount;
        if (_tickCount > 10)
        {
          // we ignore the first few ticks
          float maxRatio = _solarRotors.Max(r => r.Ratio);
          if (maxRatio > DAY_RATIO)
          {
            if (_nightMode)
            {
              _log("Entering Day Mode");
              _nightMode = false;
            }
          }
          else if (maxRatio < NIGHT_RATIO)
          {
            if (!_nightMode)
            {
              _log("Entering Night Mode");
              _nightMode = true;
            }
          }
        }
        if (_tickCount > 10)
        {
          foreach (SolarRotor rotor in _solarRotors)
          {
            rotor.Update(_nightMode);
          }
        }
      }

      void _saveRotors()
      {
        foreach (SolarRotor rotor in _solarRotors)
        {
          rotor.Save();
        }
      }

      void _update()
      {
        _saveRotors();
        _updator.Update(_solarRotors, _keyword);
      }

      void _log(string s) => _logger?.Invoke($"Solar manager: {s}");
    }
  }
}
