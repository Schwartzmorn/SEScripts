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
    public class SolarManager {
      const string SECTION = "solar-manager";
      const float DAY_RATIO = 0.8f;
      const float NIGHT_RATIO = 0.7f;

      public float CurrentOutput => this.solarRotors.Sum(r => r.CurrentOutput);
      public float MaxOutput => this.solarRotors.Sum(r => r.MaxOutput);
      public int PanelCount => this.solarRotors.Sum(r => r.PanelCount);
      public int RotorCount => this.solarRotors.Count;

      readonly string keyword;
      readonly Action<string> logger;
      readonly List<SolarRotor> solarRotors = new List<SolarRotor>();
      readonly SolarUpdator updator;
      bool nightMode;
      int tickCount = 0;

      public SolarManager(Program p, CommandLine command, ISaveManager manager, Action<string> logger) {
        this.logger = logger;
        var ini = new MyIni();
        ini.TryParse(p.Me.CustomData);
        this.keyword = ini.GetThrow(SECTION, "keyword").ToString("Solar Rotor");
        this.nightMode = ini.Get(SECTION, "night-mode").ToBoolean();
        Process main = manager.Spawn(process => this.main(), "solar-manager", period: 50);
        this.updator = new SolarUpdator(p, logger);
        this.update();
        main.Spawn(process => this.update(), "solar-manager-update", period: 10000);
        manager.AddOnSave(this.save);
        command.RegisterCommand(new Command("solar-adjust", Command.Wrap(this.adjust), "adjusts the position of a rotor", minArgs: 2, maxArgs: 3,
            detailedHelp: @"first argument is the offset in degree
second argument is the id of the rotor
third (optional) is the id of the auxilliary rotor"));
        command.RegisterCommand(new Command("solar-track", Command.Wrap(this.track), "forces an idle rotor to start tracking", nArgs: 1));
      }

      void adjust(List<string> args) {
        float offset = float.Parse(args[0]);
        int rotorId = int.Parse(args[1]);
        int rotorAuxId = (args.Count == 3) ? int.Parse(args[2]) : 0;
        this.solarRotors.FirstOrDefault(r => r.IDNumber == rotorId)?.Adjust(offset, rotorAuxId);
      }

      void track(string arg) {
        int rotorId = int.Parse(arg);
        this.solarRotors.FirstOrDefault(r => r.IDNumber == rotorId)?.Track();
      }

      void save(MyIni ini) {
        ini.Set(SECTION, "night-mode", this.nightMode);
        this.saveRotors();
      }

      void main() {
        ++this.tickCount;
        if (this.tickCount > 10) {
          // we ignore the first few ticks
          float maxRatio = this.solarRotors.Max(r => r.Ratio);
          if (maxRatio > DAY_RATIO) {
            if (this.nightMode) {
              this.log("Entering Day Mode");
              this.nightMode = false;
            }
          } else if (maxRatio < NIGHT_RATIO) {
            if (!this.nightMode) {
              this.log("Entering Night Mode");
              this.nightMode = true;
            }
          }
        }
        if (this.tickCount > 10) {
          foreach (SolarRotor rotor in this.solarRotors) {
            rotor.Update(this.nightMode);
          }
        }
      }

      void saveRotors() {
        foreach (SolarRotor rotor in this.solarRotors) {
          rotor.Save();
        }
      }

      void update() {
        this.saveRotors();
        this.updator.Update(this.solarRotors, this.keyword);
      }

      void log(string s) => this.logger?.Invoke($"Solar manager: {s}");
    }
  }
}
