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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program : MyGridProgram {
    // Settings
    static float ALIGNMENT_CUTOFF = 0.01f;
    static float VERTICAL_VELOCITY = 0.001f;
    static float HORIZONTAL_VELOCITY = 0.003f;

    // To make them visible to subclasses
    public static Action<String> ECHO;
    public static IMyGridTerminalSystem GTS;
    public static IMyProgrammableBlock ME;

    // Internals
    BatteryManager mainBatteriesManager;
    BatteryManager connectedBatteriesManager;
    DayTracker dayTracker;
    SolarDisplay display;
    SolarManager solarManager;
    SolarState solarState;
    int updateCounter;
    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update100;
    }

    public void Save() {
      Serializer serializer = new Serializer();
      solarManager.Serialize(serializer);
      serializer.Serialize("solarState", solarState.Serialize());
      dayTracker.Serialize(serializer);
      Me.CustomData = serializer.GetSerializedString();
    }

    public static float AngleProxy(float A1 = 0, float A2 = 0) => (float)Mod((double)(A1 - A2) + Math.PI, 2 * Math.PI) - (float)Math.PI;

    public static double Mod(double A, double N) => A - (Math.Floor(A / N) * N);

    private void Init() {
      // launch tests
      // TestSuite.AllTests.TestAll();
      // Initialization
      mainBatteriesManager = new BatteryManager(battery => battery.CubeGrid == Me.CubeGrid);
      connectedBatteriesManager = new BatteryManager(battery => battery.CubeGrid != Me.CubeGrid);
      display = new SolarDisplay("Main Base Solar LCD Panel", false);
      solarManager = new SolarManager("A");
      dayTracker = new DayTracker();

      Deserializer deserializer = new Deserializer(Me.CustomData);
      solarManager.Deserialize(deserializer);
      dayTracker.Deserialize(deserializer);
      solarState = SolarState.Deserialize(deserializer.Get("solarState"));
    }

    private void Tick() {
      switch(updateCounter % 3) {
        case 0:
          mainBatteriesManager.UpdateFromGrid();
          break;
        case 1:
          connectedBatteriesManager.UpdateFromGrid();
          break;
        case 2:
          solarManager.UpdateFromGrid();
          break;
      }
      ++updateCounter;
      solarManager.Tick();
      solarState = solarState.Handle(solarManager);
      dayTracker.Tick(solarState, solarManager.WitnessPosition, Runtime.TimeSinceLastRun.TotalMilliseconds);
      UpdateDisplay();
    }

    private void UpdateDisplay() {
      display.WriteCentered("Power management", LCDColor.Grey);
      display.Write("");
      display.WriteCentered("Solar power status", new LCDColor(1, 1, 2));
      display.Write(" Currently " + solarState.GetName());
      display.Write(" Output: " + SolarDisplay.FormatRatio(solarManager.MaxOutput, solarManager.MaxHistoricalOutput));
      display.DrawRatio(solarManager.MaxOutput, solarManager.MaxHistoricalOutput);
      display.Write("");
      display.Write("");
      display.WriteCentered("Main batteries status", new LCDColor(1, 1, 2));
      WriteBatteryStatus(mainBatteriesManager);
      display.Write("");
      display.Write("");
      display.WriteCentered("Connected batteries status", new LCDColor(1, 1, 2));
      WriteBatteryStatus(connectedBatteriesManager);
      display.Write("");
      display.Write("");
      display.WriteCentered("Time management", new LCDColor(1, 1, 2));
      TimeStruct time = dayTracker.GetTime();
      display.Write("Time of day: " + time.GetTime().ToString());
      display.Write("Length of day: " + time.GetDayLength().ToString());
      display.Write(solarManager.GetOutputHistory());
      display.Flush();
    }

    private void WriteBatteryStatus(BatteryManager batteryManager) {
      display.Write(" Charge: " + SolarDisplay.FormatRatio(batteryManager.CurrentCharge, batteryManager.MaxCharge));
      display.DrawRatio(batteryManager.CurrentCharge, batteryManager.MaxCharge);
      display.Write(" Input: " + SolarDisplay.FormatRatio(batteryManager.CurentInput, batteryManager.MaxInput));
      display.DrawRatio(batteryManager.CurentInput, batteryManager.MaxInput);
      display.Write(" Output: " + SolarDisplay.FormatRatio(batteryManager.CurentOutput, batteryManager.MaxOutput));
      display.DrawRatio(batteryManager.CurentOutput, batteryManager.MaxOutput, true);
    }

    public void Main(string argument, UpdateType updateSource) {
      ME = Me;
      GTS = GridTerminalSystem;
      ECHO = Echo;
      try {
        if (solarManager == null) {
          Init();
        }
        if (String.IsNullOrEmpty(argument)) {
          Tick();
        } else {
          if (argument == "reset") {
            solarState = SolarState.Transition(solarState, new SolarStateReseting(), solarManager);
          } else if (argument == "idle") {
            solarState = SolarState.Transition(solarState, new SolarStateIdlingDay(), solarManager);
          } else if (argument.StartsWith("addPylon")) {
            string[] args = argument.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < args.Length; ++i) {
              solarManager.AddPylon(args[i]);
            }
          }
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