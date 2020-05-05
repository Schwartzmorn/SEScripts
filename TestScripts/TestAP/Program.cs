using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
  partial class Program : MyGridProgram {

    // Isy's Solar Alignment Script
    // ============================
    // Version: 4.3.1
    // Date: 2019-10-13

    // =======================================================================================
    //                                                                            --- Configuration ---
    // =======================================================================================

    // --- Essential Configuration ---
    // =======================================================================================

    // Name of the group with all the solar related rotors (not needed in gyro mode)
    string rotorGroupName = "Solar Rotors";

    // By enabling gyro mode, the script will no longer use rotors but all gyroscopes on the grid instead.
    // This mode only makes sense when used on a SHIP in SPACE. Gyro mode deactivates the following
    // features: night mode, rotate to sunrise, time calculation and triggering external timer blocks.
    bool useGyroMode = false;

    // Name of the reference group for gyro mode. Put your main cockpit, flight seat or remote control in this group!
    string referenceGroupName = "Solar Reference";

    // --- Rotate to sunrise ---
    // =======================================================================================

    // Rotate the panels towards the sunrise during the night? (Possible values: true | false, default: true)
    // The angle is figured out automatically based on the first lock of the day.
    // If you want to set the angles yourself, set manualAngle to true and adjust the angles to your likings.
    bool rotateToSunrise = true;
    bool manualAngle = false;
    int manualAngleVertical = 0;
    int manualAngleHorizontal = 0;

    // --- Power fallback ---
    // =======================================================================================

    // With this option, you can enable your reactors and hydrogen engines as a safety fallback, if not enough power is available
    // to power all your machines or if the battery charge gets low. By default, all reactors and hydrogen engines
    // on the same grid will be used. If you only want to use specific ones, put their names or group in the list.
    // Example: string[] fallbackDevices = { "Small Reactor 1", "Base reactor group", "Hydrogen Engine" };
    bool useReactorFallback = false;
    bool useHydrogenEngineFallback = false;
    string[] fallbackDevices = { };

    // Activation order
    // By default, the hydrogen engine will be turned on first and the reactors after that if still not enough power is available.
    // Set this value to false, and the reactors will be used first.
    bool activateHydrogenEngineFirst = true;

    // Activation on low battery?
    // The fallback devices will be active until 'turnOffAtPercent' of the max battery charge after it was turned on at 'turnOnAtPercent'.
    bool activateOnLowBattery = true;
    double turnOnAtPercent = 10;
    double turnOffAtPercent = 15;

    // Activate on overload?
    // If the combined output of batteries, solar panels and wind turbines is more than 'overloadPercentage' of their max output, the fallback devices will be turned on.
    bool activateOnOverload = true;
    double overloadPercentage = 90;

    // --- Base Light Management ---
    // =======================================================================================

    // Enable base light management? (Possible values: true | false, default: false)
    // Lights will be turned on/off based on daytime.
    bool baseLightManagement = false;

    // Simple mode: toggle lights based on max. solar output (percentage). Time based toggle will be deactivated.
    bool simpleMode = false;
    int simpleThreshold = 50;

    // Define the times when your lights should be turned on or off. If simple mode is active, this does nothing.
    int lightOffHour = 8;
    int lightOnHour = 18;

    // To only toggle specific lights, declare groups for them.
    // Example: string[] baseLightGroups = { "Interior Lights", "Spotlights", "Hangar Lights" };
    string[] baseLightGroups = { };

    // --- LCD panels ---
    // =======================================================================================

    // To display the main script informations, add the following keyword to any LCD name (default: !ISA-main).
    // You can enable or disable specific informations on the LCD by editing its custom data.
    string mainLcdKeyword = "!ISA-main";

    // To display compact stats (made for small screens, add the following keyword to any LCD name (default: !ISA-compact).
    string compactLcdKeyword = "!ISA-compact";

    // To display all current warnings and problems, add the following keyword to any LCD name (default: !ISA-warnings).
    string warningsLcdKeyword = "!ISA-warnings";

    // To display the script performance, add the following keyword to any LCD name (default: !ISA-performance).
    string performanceLcdKeyword = "!ISA-performance";

    // Default font ("Debug" or "Monospace") and fontsize for new LCDs
    string defaultFont = "Debug";
    float defaultFontSize = 0.6f;

    // --- Terminal statistics ---
    // =======================================================================================

    // The script can display informations in the names of the used blocks. The shown information is a percentage of
    // the current efficiency (solar panels and oxygen farms) or the fill level (batteries and tanks).
    // You can enable or disable single statistics or disable all using the master switch below.
    bool enableTerminalStatistics = true;

    bool showSolarStats = true;
    bool showWindTurbineStats = true;
    bool showBatteryStats = true;
    bool showOxygenFarmStats = true;
    bool showOxygenTankStats = true;

    // --- External timer blocks ---
    // =======================================================================================

    // Trigger external timer blocks at specific events? (action "Start" will be applied which takes the delay into account)
    // Events can be: "sunrise", "sunset", a time like "15:00" or a number for every X seconds
    // Every event needs a timer block name in the exact same order as the events.
    // Calling the same timer block with multiple events requires it's name multiple times in the timers list!
    // Example:
    // string[] events = { "sunrise", "sunset", "15:00", "30" };
    // string[] timers = { "Timer 1", "Timer 1", "Timer 2", "Timer 3" };
    // This will trigger "Timer 1" at sunrise and sunset, "Timer 2" at 15:00 and "Timer 3" every 30 seconds.
    bool triggerTimerBlock = false;
    string[] events = { };
    string[] timers = { };

    // --- Settings for enthusiasts ---
    // =======================================================================================

    // Change percentage of the last locked output where the script should realign for a new best output (default: 1, gyro: 5)
    double realginPercentageRotor = 1;
    double realignPercentageGyro = 5;

    // Percentage of the max detected output where the script starts night mode (default: 10)
    double nightPercentage = 10;

    // Percentage of the max detected output where the script detects night for time calculation (default: 50)
    double nightTimePercentage = 50;

    // Rotor speeds (speeds are automatically scaled between these values based on the output)
    const float rotorMinSpeed = 0.05f;
    const float rotorMaxSpeed = 1.0f;

    // Rotor options
    float rotorTorqueLarge = 33600000f;
    float rotorTorqueSmall = 448000f;
    bool setInertiaTensor = true;
    bool setRotorLockWhenStopped = false;

    // Min gyro RPM, max gyro RPM and gyro power for gyro mode
    const double minGyroRPM = 0.1;
    const double maxGyroRPM = 1;
    const float gyroPower = 1f;

    // =======================================================================================
    //                                                                      --- End of Configuration ---
    //                                                        Don't change anything beyond this point!
    // =======================================================================================

    float maxOutput = 0;
    float bestOutput = 0;
    float maxSolarOutput = 0;
    float outputLast = 0;
    float maxDetectedOutput = 0;
    float currentSolarOutput = 0;
    List<IMyMotorStator> allRotors = new List<IMyMotorStator> ();
    List<IMyMotorStator> verticalRotors = new List<IMyMotorStator> ();
    List<IMyMotorStator> horizontalRotors = new List<IMyMotorStator> ();
    List<IMyGyro> gyros = new List<IMyGyro> ();
    List<IMyTextPanel> displays = new List<IMyTextPanel> ();
    List<IMyTerminalBlock> mainLCDs = new List<IMyTerminalBlock> ();
    List<IMyTerminalBlock> compactLCDs = new List<IMyTerminalBlock> ();
    List<IMyTerminalBlock> warningLCDs = new List<IMyTerminalBlock> ();
    List<IMyTerminalBlock> performanceLCDs = new List<IMyTerminalBlock> ();
    List<IMyInteriorLight> lights = new List<IMyInteriorLight> ();
    List<IMyReflectorLight> spotlights = new List<IMyReflectorLight> ();
    List<IMyPowerProducer> fallbackPowerProducers = new List<IMyPowerProducer> ();
    int Ǔ = 0;
    int ǔ = 0;
    int Ǖ = 0;
    int ǖ = 0;
    List<IMySolarPanel> solarPanels = new List<IMySolarPanel> ();
    int solarPanelsCount = 0;
    bool isNight = false;
    bool ǚ = false;
    int ǜ = 30;
    int ǥ = 90;
    int restPeriod = 10;
    bool shouldResetMotors = true;
    List<string> defaultState = new List<string> {
      "output=0",
      "outputLast=0",
      "outputLocked=0",
      "outputMax=0",
      "outputMaxAngle=0",
      "outputMaxDayBefore=0",
      "outputBestPanel=0",
      "direction=1",
      "directionChanged=0",
      "directionTimer=0",
      "allowRotation=1",
      "rotationDone=1",
      "timeSinceRotation=0",
      "firstLockOfDay=0",
      "sunriseAngle=0"
    };
    List<IMyShipController> shipControllers = new List<IMyShipController> ();
    double ǡ = 0;
    double Ǣ = 0;
    double ǣ = 0;
    double Ǥ = 1;
    double Ǧ = 1;
    double Ǒ = 1;
    bool ǐ = false;
    bool Ƒ = false;
    bool Ɣ = false;
    double ƕ = 0;
    double Ɩ = 0;
    double Ɨ = 0;
    bool Ƙ = true;
    bool ƙ = true;
    bool ƚ = true;
    double ƛ = 0;
    double Ɯ = 0;
    double Ɲ = 0;
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock> ();
    float batteryCurrentInput = 0;
    float batteryMaxInput = 0;
    float batteryCurrentOutput = 0;
    float batteryMaxOutput = 0;
    float batteryCurrentStoredPower = 0;
    float batteryMaxStoredPower = 0;
    List<IMyOxygenFarm> oxygenFarms = new List<IMyOxygenFarm> ();
    List<IMyGasTank> gasTanks = new List<IMyGasTank> ();
    double oxygenOutput = 0;
    double oxygenCapacity = 0;
    double oxygenFillRatio = 0;
    int oxygenFarmsCount = 0;
    List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer> ();
    float windCurrentOutput = 0;
    float windMaxOutput = 0;
    string maxSolarOutputString = "0 kW";
    string maxDetectedOutputString = "0 kW";
    string currentSolarOutputString = "0 kW";
    string batteryCurrentInputString = "0 kW";
    string batteryMaxInputString = "0 kW";
    string batteryCurrentOutputString = "0 kW";
    string batteryMaxOutputString = "0 kW";
    string batteryCurrentStoredPowerString = "0 kW";
    string batteryMaxStoredPowerString = "0 kW";
    string windCurrentOutputString = "0 kW";
    string windMaxOutputString = "0 kW";
    string oxygenCapacityString = "0 L";
    string oxygenFillRatioString = "0 L";
    string currentActionString = "Checking setup...";
    string currentStatusString;
    string statusString;
    string[] doodleSequence = { "/", "-", "\\", "|" };
    int doodleCount = 0;
    int dayTimer = 0;
    int ƽ = 270;
    const int defaultDayLength = 7200;
    int dayLength = defaultDayLength;
    const int defaultDaytimeLength = defaultDayLength / 2;
    int sunSet = defaultDaytimeLength;
    string[] mainLCDDefaultOptions = {
      "showHeading=true",
      "showWarnings=true",
      "showCurrentOperation=true",
      "showSolarStats=true",
      "showTurbineStats=true",
      "showBatteryStats=true",
      "showOxygenStats=true",
      "showLocationTime=true",
      "scrollTextIfNeeded=true"
    };
    string[] compactLCDDefaultOptions = {
      "showSolarStats=false",
      "showTurbineStats=false",
      "showBatteryStats=false",
      "showOxygenStats=false",
      "showLocationTime=false",
      "showRealTime=false",
      "scrollTextIfNeeded=false"
    };
    string[] warningLCDsDefaultOptions = {
      "showHeading=true",
      "scrollTextIfNeeded=true"
    };
    bool Ǆ = false;
    bool ǅ = false;
    bool ǆ = false;
    int reactorCount = 0;
    int hydrogenEngineCount = 0;
    double ǉ = 0;
    string Ǌ = "";
    string warning;
    int errorCount = 0;
    bool lastRunInError = false;
    HashSet<string> previousWarnings = new HashSet<string> ();
    HashSet<string> currentWarnings = new HashSet<string> ();
    string action = "align";
    int ƪ = 3;
    bool Ƴ = false;
    string ƫ = "both";
    bool Ƭ = false;
    double ƭ = 0;
    double Ʈ = 0;
    int Ư = 0;
    int ư = 0;
    int step = 1;
    bool initializing = true;
    bool isUpdatingDisplays = true;
    int displayCounter = 0;
    string[] stepNames = { "Get blocks", "Get block stats", "Time Calculation", "Rotation Logic", "Reactor Fallback" };

    Program () {
      this.Deserialize ();
      this.realginPercentageRotor = (this.realginPercentageRotor % 100) / 100;
      this.realignPercentageGyro = (this.realignPercentageGyro % 100) / 100;
      this.nightPercentage = (this.nightPercentage % 100) / 100;
      this.nightTimePercentage = (this.nightTimePercentage % 100) / 100;
      this.Runtime.UpdateFrequency = UpdateFrequency.Update10;
    }
    void Main (string args) {
      if (this.errorCount >= 10) {
        throw new Exception ("Too many errors. Please recompile!\n\nScript stoppped!\n");
      }
      try {
        this.fetchPerformances ("", true);
        if (args != "") {
          this.action = args.ToLower ();
          this.step = 3;
        }
        if (this.initializing) {
          this.initBlocks ();
          this.resetMotors (false);
          this.resetAll ();
          this.initializing = false;
        }
        if (this.ư < this.Ư) {
          this.ư++;
          return;
        }
        if (this.isUpdatingDisplays) {
          if (this.displayCounter == 0)
            this.updateMainDisplays ();
          if (this.displayCounter == 1)
            this.updateCompactDisplays ();
          if (this.displayCounter == 2)
            this.updateWarningDisplays ();
          if (this.displayCounter == 3)
            this.updatePerformanceDisplays ();
          if (this.displayCounter > 3)
            this.displayCounter = 0;
          this.isUpdatingDisplays = false;
          return;
        }
        if (this.step == 0 || this.lastRunInError) {
          this.initBlocks ();
          this.lastRunInError = false;
          if (this.step == 0) {
            this.fetchPerformances (this.stepNames[this.step]);
            this.echoDebug ();
            this.step++;
          }
          return;
        }
        this.ư = 0;
        this.isUpdatingDisplays = true;
        if (this.step == 1) {
          this.updateSolarOutput ();
        }
        if (this.step == 2 && !this.useGyroMode) {
          this.timeRelatedStuff ();
          if (this.baseLightManagement) this.handleLights ();
          if (this.triggerTimerBlock) this.handleTimerBlock ();
        }
        if (this.step == 3) {
          if (!this.handleManualAction (this.action)) {
            if (this.useGyroMode) {
              this.alignGyros();
            } else {
              this.alignAllRotors();
            }
          }
        }
        if (this.step == 4) {
          if (this.useReactorFallback || this.useHydrogenEngineFallback)
            this.handlePowerFallback ();
          foreach (var rotorA in this.allRotors) {
            double output = this.getDoubleValue (rotorA, "output");
            this.saveValue (rotorA, "outputLast", output);
          }
          this.outputLast = this.maxSolarOutput;
          this.Save ();
        }
        this.fetchPerformances (this.stepNames[this.step]);
        this.echoDebug ();
        if (this.step >= 4) {
          this.step = 0;
          this.previousWarnings = new HashSet<string> (this.currentWarnings);
          this.currentWarnings.Clear ();
          if (this.errorCount > 0)
            this.errorCount--;
          if (this.previousWarnings.Count == 0)
            this.warning = null;
          this.statusString = this.currentStatusString;
          this.currentStatusString = "";
        } else {
          this.step++;
        }
        this.doodleCount = this.doodleCount >= 3 ? 0 : this.doodleCount + 1;
      } catch (NullReferenceException) {
        this.errorCount++;
        this.lastRunInError = true;
        this.log ("Execution of script step aborted:\n" + this.stepNames[this.step] + " (ID: " + this.step + ")\n\nCached block not available..");
      } catch (Exception e) {
        this.errorCount++;
        this.lastRunInError = true;
        this.log ("Critical error in script step:\n" + this.stepNames[this.step] + " (ID: " + this.step + ")\n\n" + e);
      }
    }

    void initBlocks () {
      if (this.Ŧ == null) { this.Ũ(this.Me.CubeGrid); }
      if (!this.useGyroMode) {
        var rotorGroup = this.GridTerminalSystem.GetBlockGroupWithName (this.rotorGroupName);
        if (rotorGroup != null) {
          rotorGroup.GetBlocksOfType<IMyMotorStator> (this.allRotors);
          if (this.allRotors.Count == 0) {
            this.log ("There are no rotors in the rotor group:\n'" + this.rotorGroupName + "'");
          }
        } else {
          this.log ("Rotor group not found:\n'" + this.rotorGroupName + "'");
        }
        HashSet<IMyCubeGrid> rotorGrids = new HashSet<IMyCubeGrid> ();
        foreach (var rotor in this.allRotors) {
          if (!rotor.IsFunctional)
            this.log ("'" + rotor.CustomName + "' is broken!\nRepair it to use it for aligning!");
          if (!rotor.Enabled)
            this.log ("'" + rotor.CustomName + "' is turned off!\nTurn it on to use it for aligning!");
          if (!rotor.IsAttached)
            this.log ("'" + rotor.CustomName + "' has no rotor head!\nAdd one to use it for aligning!");
          rotorGrids.Add (rotor.CubeGrid);
          if (rotor.CubeGrid.GridSize == 0.5) {
            rotor.Torque = this.rotorTorqueSmall;
          } else {
            rotor.Torque = this.rotorTorqueLarge;
          }
          if (rotor.GetOwnerFactionTag () != this.Me.GetOwnerFactionTag ()) {
            this.log ("'" + rotor.CustomName + "' has a different owner / faction!\nAll blocks should have the same owner / faction!");
          }
        }
        this.verticalRotors.Clear ();
        this.horizontalRotors.Clear ();
        foreach (var rotor in this.allRotors) {
          if (rotorGrids.Contains (rotor.TopGrid)) {
            this.verticalRotors.Add (rotor);
          } else {
            this.horizontalRotors.Add (rotor);
            if (rotor.CubeGrid != this.Ŧ && this.setInertiaTensor) {
              try {
                rotor.SetValueBool ("ShareInertiaTensor", true);
              } catch (Exception) { }
            }
          }
        }
        List<IMyMotorStator> auxRotors = new List<IMyMotorStator> ();
        auxRotors.AddRange (this.horizontalRotors);
        this.horizontalRotors.Clear ();
        bool keep;
        // gets rid of redundant rotors
        foreach (var rotor in auxRotors) {
          keep = true;
          foreach (var keptRotor in this.horizontalRotors) {
            if (keptRotor.TopGrid == rotor.TopGrid) {
              rotor.RotorLock = false;
              rotor.TargetVelocityRPM = 0f;
              rotor.Torque = 0f;
              rotor.BrakingTorque = 0f;
              keep = false;
              break;
            }
          }
          if (keep)
            this.horizontalRotors.Add (rotor);
        }
        this.solarPanels.Clear ();
        this.oxygenFarms.Clear ();
        foreach (var rotor in this.horizontalRotors) {
          this.maxOutput = 0;
          this.bestOutput = 0;
          this.initAttachedGrids (rotor.TopGrid, true);
          var panels = new List<IMySolarPanel> ();
          this.GridTerminalSystem.GetBlocksOfType<IMySolarPanel> (panels, p => this.attachedGrids.Contains (p.CubeGrid) && p.IsWorking);
          var farms = new List<IMyOxygenFarm> ();
          this.GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm> (farms, f => this.attachedGrids.Contains (f.CubeGrid) && f.IsWorking);
          foreach (var panel in panels) {
            this.solarPanels.Add (panel);
            this.maxOutput += panel.MaxOutput;
            if (panel.MaxOutput > this.bestOutput) this.bestOutput = panel.MaxOutput;
          }
          foreach (var f in farms) {
            this.oxygenFarms.Add (f);
            this.maxOutput += f.GetOutput ();
            if (f.GetOutput () > this.bestOutput) this.bestOutput = f.GetOutput ();
          }
          if (panels.Count == 0 && farms.Count == 0) {
            this.log ("'" + rotor.CustomName + "' can't see the sun!\nAdd a solar panel or oxygen farm to it!");
          }
          this.saveValue (rotor, "output", this.maxOutput);
          this.saveValue (rotor, "outputBestPanel", this.bestOutput);
          if (this.maxOutput > this.getDoubleValue (rotor, "outputMax")) {
            this.saveValue (rotor, "outputMax", this.maxOutput);
            this.saveValue (rotor, "outputMaxAngle",
              this.getAngleDegrees (rotor));
          }
        }
        foreach (var rotor in this.verticalRotors) {
          double output = 0;
          this.bestOutput = float.MaxValue;
          foreach (var auxRotor in this.horizontalRotors) {
            if (auxRotor.CubeGrid == rotor.TopGrid) {
              output += this.getDoubleValue (auxRotor, "output");
              if (this.getDoubleValue (auxRotor, "outputBestPanel") < this.bestOutput)
                this.bestOutput = (float) this.getDoubleValue (auxRotor, "outputBestPanel");
            }
          }
          this.saveValue (rotor, "output", output);
          this.saveValue (rotor, "outputBestPanel", this.bestOutput);
          if (output > this.getDoubleValue (rotor, "outputMax")) {
            this.saveValue (rotor, "outputMax", output);
            this.saveValue (rotor, "outputMaxAngle", this.getAngleDegrees (rotor));
          }
        }
      }
      if (this.useGyroMode) {
        if (this.Me.CubeGrid.IsStatic) {
          this.log ("The grid is stationary!\nConvert it to a ship in the Info tab!");
        }
        var Ƚ = this.GridTerminalSystem.GetBlockGroupWithName (this.referenceGroupName);
        if (Ƚ != null) {
          Ƚ.GetBlocksOfType<IMyShipController> (this.shipControllers);
          if (this.shipControllers.Count == 0) {
            this.log ("There are no cockpits, flight seats or remote controls in the reference group:\n'" + this.referenceGroupName + "'");
          }
        } else {
          this.log ("Reference group not found!\nPut your main cockpit, flight seat or remote control in a group called '" + this.referenceGroupName + "'!");
        }
        this.GridTerminalSystem.GetBlocksOfType<IMyGyro> (this.gyros, Ȩ => Ȩ.IsSameConstructAs (this.Me) && Ȩ.IsWorking);
        if (this.gyros.Count == 0) {
          this.log ("No gyroscopes found!\nAre they enabled and completely built?");
        }
        this.GridTerminalSystem.GetBlocksOfType<IMySolarPanel> (this.solarPanels, Ⱥ => Ⱥ.IsSameConstructAs (this.Me) && Ⱥ.IsWorking);
        this.GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm> (this.oxygenFarms, ȼ => ȼ.IsSameConstructAs (this.Me) && ȼ.IsWorking);
      }
      if (this.solarPanelsCount != this.solarPanels.Count || this.oxygenFarmsCount != this.oxygenFarms.Count) {
        foreach (var rotor in this.allRotors) {
          this.saveValue (rotor, "outputMax", 0);
        }
        this.maxDetectedOutput = 0;
        this.solarPanelsCount = this.solarPanels.Count;
        this.oxygenFarmsCount = this.oxygenFarms.Count;
        this.log ("Amount of solar panels or oxygen farms changed!\nRestarting..");
      }
      if (this.solarPanels.Count == 0 && this.oxygenFarms.Count == 0) {
        this.log ("No solar panels or oxygen farms found!\nHow should I see the sun now?");
      }
      this.batteries.Clear ();
      this.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock> (this.batteries, ũ => ũ.IsSameConstructAs (this.Me) && ũ.IsWorking);
      if (this.batteries.Count == 0) {
        this.log ("No batteries found!\nDon't you want to store your Power?");
      }
      this.windTurbines.Clear ();
      this.GridTerminalSystem.GetBlocksOfType<IMyPowerProducer> (this.windTurbines, ȵ => ȵ.BlockDefinition.TypeIdString.Contains ("WindTurbine") && ȵ.IsSameConstructAs (this.Me) && ȵ.IsWorking);
      this.gasTanks.Clear ();
      this.GridTerminalSystem.GetBlocksOfType<IMyGasTank> (this.gasTanks, ȵ => !ȵ.BlockDefinition.SubtypeId.Contains ("Hydrogen") && ȵ.IsSameConstructAs (this.Me) && ȵ.IsWorking);
      if (this.useReactorFallback || this.useHydrogenEngineFallback) {
        this.fallbackPowerProducers.Clear ();
        foreach (var P in this.fallbackDevices) {
          var Ȫ = this.GridTerminalSystem.GetBlockGroupWithName (P);
          if (Ȫ != null) {
            var ȧ = new List<IMyPowerProducer> ();
            Ȫ.GetBlocksOfType<IMyPowerProducer> (ȧ, Ȩ => Ȩ.BlockDefinition.TypeIdString.Contains ("Reactor") || Ȩ.BlockDefinition.TypeIdString.Contains ("HydrogenEngine"));
            this.fallbackPowerProducers.AddRange (ȧ);
          } else {
            IMyPowerProducer ȩ = this.GridTerminalSystem.GetBlockWithName (P) as IMyPowerProducer;
            if (ȩ != null) { this.fallbackPowerProducers.Add (ȩ); } else { this.log ("Power fallback device not found:\n'" + P + "'"); }
          }
        }
        if (this.fallbackPowerProducers.Count == 0) {
          this.GridTerminalSystem.GetBlocksOfType<IMyPowerProducer> (this.fallbackPowerProducers, Ȩ => (Ȩ.BlockDefinition.TypeIdString.Contains ("Reactor") || Ȩ.BlockDefinition.TypeIdString.Contains ("HydrogenEngine")) && Ȩ.IsSameConstructAs (this.Me) && Ȩ.IsFunctional);
        }
        if (!this.useReactorFallback)
          this.fallbackPowerProducers.RemoveAll (Ȩ => Ȩ.BlockDefinition.TypeIdString.Contains ("Reactor"));
        if (!this.useHydrogenEngineFallback)
          this.fallbackPowerProducers.RemoveAll (Ȩ => Ȩ.BlockDefinition.TypeIdString.Contains ("HydrogenEngine"));
        this.reactorCount = this.fallbackPowerProducers.Count (Ȩ => Ȩ.BlockDefinition.TypeIdString.Contains ("Reactor"));
        this.hydrogenEngineCount = this.fallbackPowerProducers.Count (Ȩ => Ȩ.BlockDefinition.TypeIdString.Contains ("HydrogenEngine"));
      }
      if (this.baseLightManagement) {
        this.lights.Clear ();
        this.spotlights.Clear ();
        if (this.baseLightGroups.Length > 0) {
          var ȫ = new List<IMyInteriorLight> ();
          var ȳ = new List<IMyReflectorLight> ();
          foreach (var Ȭ in this.baseLightGroups) {
            var ȭ = this.GridTerminalSystem.GetBlockGroupWithName (Ȭ);
            if (ȭ != null) {
              ȭ.GetBlocksOfType<IMyInteriorLight> (ȫ);
              this.lights.AddRange (ȫ);
              ȭ.GetBlocksOfType<IMyReflectorLight> (ȳ);
              this.spotlights.AddRange (ȳ);
            } else { this.log ("Light group not found:\n'" + Ȭ + "'"); }
          }
        } else {
          this.GridTerminalSystem.GetBlocksOfType<IMyInteriorLight> (this.lights, Ȯ => Ȯ.IsSameConstructAs (this.Me));
          this.GridTerminalSystem.GetBlocksOfType<IMyReflectorLight> (this.spotlights, Ȯ => Ȯ.IsSameConstructAs (this.Me));
        }
      }
      this.mainLCDs = this.findDisplayBlocks (this.mainLcdKeyword, this.mainLCDDefaultOptions);
      this.compactLCDs = this.findDisplayBlocks (this.compactLcdKeyword, this.compactLCDDefaultOptions);
      this.warningLCDs = this.findDisplayBlocks (this.warningsLcdKeyword, this.warningLCDsDefaultOptions);
      this.performanceLCDs = this.findDisplayBlocks (this.performanceLcdKeyword, this.warningLCDsDefaultOptions);
    }
    bool handleManualAction (string action) {
      bool done = true;
      if (action == "pause") {
        this.resetMotors ();
        if (this.Ƴ) {
          this.action = "align";
          this.Ƴ = false;
          return false;
        } else { this.action = "paused"; this.Ƴ = true; }
        this.currentActionString = "Automatic alignment paused.\n";
        this.currentActionString += "Run 'pause' again to continue..";
      } else if (action == "paused") { this.currentActionString = "Automatic alignment paused.\n"; this.currentActionString += "Run 'pause' again to continue.."; } else if (action == "realign" && !
        this.useGyroMode) {
        this.ȝ();
        this.currentActionString = "Forced realign by user.\n";
        this.currentActionString += "Searching highest output for " + this.ǥ + " more seconds.";
        if (this.ǥ == 0) { this.action = ""; this.ǥ = 90; } else {
          this.ǥ -= 1;
        }
      } else if (action == "reset" && !this.useGyroMode) {
        this.dayTimer = 0;
        this.ƽ = 270;
        this.sunSet = defaultDaytimeLength;
        this.dayLength = defaultDayLength;
        this.currentActionString = "Calculated time resetted.\n";
        this.currentActionString += "Continuing in " + this.ƪ + " seconds.";
        if (this.ƪ == 0) { this.action = ""; this.ƪ = 3; } else { this.ƪ -= 1; }
      } else if (action.Contains ("rotate") && !this.useGyroMode) {
        String[] Ȳ = action.Split (' ');
        bool Ⱦ = false;
        this.ƫ = "both";
        this.Ƭ = false;
        if (Ȳ[0].Contains ("pause"))
          this.Ƭ = true;
        if (Ȳ.Length == 2) {
          if (Ȳ[1].Contains ("h")) {
            Ⱦ = Double.TryParse (Ȳ[1].Replace ("h", ""), out this.ƭ);
            this.ƫ = "horizontalOnly";
          } else if (Ȳ[1].Contains ("v")) { Ⱦ = Double.TryParse (Ȳ[1].Replace ("v", ""), out this.Ʈ); this.ƫ = "verticalOnly"; }
          if (Ⱦ) {
            this.currentActionString = "Checking rotation parameters...";
            this.action = "rotNormal";
          } else { this.resetMotors (); this.log ("Wrong format!\n\nShould be (e.g. 90 degrees):\nrotate h90 OR\nrotate v90"); }
        } else if (Ȳ.Length == 3) {
          string ȴ = "rotNormal";
          if (Ȳ[1].Contains ("v")) {
            Ⱦ = Double.TryParse (Ȳ[1].Replace ("v", ""), out this.Ʈ);
            if (Ⱦ)
              Ⱦ = Double.TryParse (Ȳ[2].Replace ("h", ""), out this.ƭ);
            ȴ = "rotVH1";
          } else {
            Ⱦ = Double.TryParse (Ȳ[1].Replace ("h", ""), out this.ƭ);
            if (Ⱦ)
              Ⱦ = Double.TryParse (Ȳ[2].Replace ("v", ""), out this.Ʈ);
          }
          if (Ⱦ) { this.currentActionString = "Checking rotation parameters..."; this.action = ȴ; } else {
            this.resetMotors ();
            this.log ("Wrong format!\n\nShould be (e.g. 90 degrees):\nrotate h90 v90 OR\nrotate v90 h90");
          }
        } else { this.resetMotors (); this.log ("Not enough parameters!\n\nShould be 2 or 3:\nrotate h90 OR\nrotate h90 v90"); }
      } else if (action == "rotNormal") {
        this.currentActionString = "Rotating to user defined values...";
        bool ȃ = this.rotateRotors(this.ƫ, this.ƭ, this.Ʈ);
        if (ȃ && this.Ƭ) {
          this.action = "paused";
        } else if (ȃ && !this.Ƭ) { this.action = "resume"; }
      } else if (action == "rotVH1") {
        this.currentActionString = "Rotating to user defined values...";
        bool ȃ = this.rotateRotors("verticalOnly", this.ƭ, this.Ʈ);
        if (ȃ) this.action = "rotVH2";
      } else if (action == "rotVH2") {
        this.currentActionString = "Rotating to user defined values...";
        bool ȃ = this.rotateRotors("horizontalOnly", this.ƭ, this.Ʈ);
        if (ȃ && this.Ƭ) { this.action = "paused"; } else if (ȃ && !this.Ƭ) { this.action = "resume"; }
      } else { done = false; }
      return done;
    }
    double getDoubleValue (IMyTerminalBlock block, string key) {
      this.initBlockState (block);
      var lines = block.CustomData.Split ('\n').ToList ();
      int lineNo = lines.FindIndex (l => l.StartsWith (key + "="));
      if (lineNo > -1) {
        return Convert.ToDouble (lines[lineNo].Replace (key + "=", ""));
      }
      return 0;
    }
    void saveValue (IMyTerminalBlock block, string key, double value) {
      this.initBlockState (block);
      var lines = block.CustomData.Split ('\n').ToList ();
      int lineNo = lines.FindIndex (l => l.StartsWith (key + "="));
      if (lineNo > -1) {
        lines[lineNo] = key + "=" + value;
        block.CustomData = String.Join ("\n", lines);
      }
    }
    void initBlockState (IMyTerminalBlock block) {
      var lines = block.CustomData.Split ('\n').ToList ();
      if (lines.Count != this.defaultState.Count) {
        block.CustomData = String.Join ("\n", this.defaultState);
      }
    }
    void updateSolarOutput () {
      this.maxSolarOutput = 0;
      this.currentSolarOutput = 0;
      foreach (var panel in this.solarPanels) {
        this.maxSolarOutput += panel.MaxOutput;
        this.currentSolarOutput += panel.CurrentOutput;
        if (this.showSolarStats && this.enableTerminalStatistics) {
          double maxOutput = 0;
          double.TryParse (panel.CustomData, out maxOutput);
          if (maxOutput < panel.MaxOutput) {
            maxOutput = panel.MaxOutput;
            panel.CustomData = maxOutput.ToString ();
          }
          this.updateName (panel, true, "", panel.MaxOutput, maxOutput);
        }
      }
      foreach (var Ô in this.oxygenFarms) {
        this.maxSolarOutput += Ô.GetOutput () / 1000000;
      }
      if (this.maxSolarOutput > this.maxDetectedOutput) {
        this.maxDetectedOutput = this.maxSolarOutput;
      }
      this.maxSolarOutputString = this.maxSolarOutput.Format ();
      this.currentSolarOutputString = this.currentSolarOutput.Format ();
      this.maxDetectedOutputString = this.maxDetectedOutput.Format ();
      this.batteryCurrentInput = 0;
      this.batteryMaxInput = 0;
      this.batteryCurrentOutput = 0;
      this.batteryMaxOutput = 0;
      this.batteryCurrentStoredPower = 0;
      this.batteryMaxStoredPower = 0;
      foreach (var battery in this.batteries) {
        this.batteryCurrentInput += battery.CurrentInput;
        this.batteryMaxInput += battery.MaxInput;
        this.batteryCurrentOutput += battery.CurrentOutput;
        this.batteryMaxOutput += battery.MaxOutput;
        this.batteryCurrentStoredPower += battery.CurrentStoredPower;
        this.batteryMaxStoredPower += battery.MaxStoredPower;
        if (this.showBatteryStats && this.enableTerminalStatistics) {
          string status = "";
          if (battery.CurrentStoredPower < battery.MaxStoredPower * 0.99) {
            status = "Draining";
            if (battery.CurrentInput > battery.CurrentOutput)
              status = "Recharging";
          }
          this.updateName (battery, true, status, battery.CurrentStoredPower, battery.MaxStoredPower);
        }
      }
      this.batteryCurrentInputString = this.batteryCurrentInput.Format ();
      this.batteryMaxInputString = this.batteryMaxInput.Format ();
      this.batteryCurrentOutputString = this.batteryCurrentOutput.Format ();
      this.batteryMaxOutputString = this.batteryMaxOutput.Format ();
      this.batteryCurrentStoredPowerString = this.batteryCurrentStoredPower.Format (true);
      this.batteryMaxStoredPowerString = this.batteryMaxStoredPower.Format (true);
      this.windCurrentOutput = 0;
      this.windMaxOutput = 0;
      foreach (var turbine in this.windTurbines) {
        this.windCurrentOutput += turbine.CurrentOutput;
        this.windMaxOutput += turbine.MaxOutput;
        if (this.showWindTurbineStats && this.enableTerminalStatistics) {
          this.updateName (turbine, true, "", turbine.CurrentOutput, turbine.MaxOutput);
        }
      }
      this.windCurrentOutputString = this.windCurrentOutput.Format ();
      this.windMaxOutputString = this.windMaxOutput.Format ();
      this.oxygenOutput = 0;
      this.oxygenCapacity = 0;
      this.oxygenFillRatio = 0;
      foreach (var farm in this.oxygenFarms) {
        this.oxygenOutput += farm.GetOutput ();
        if (this.showOxygenFarmStats && this.enableTerminalStatistics) {
          this.updateName (farm, true, "", farm.GetOutput (), 1);
        }
      }
      this.oxygenOutput = Math.Round (this.oxygenOutput / this.oxygenFarms.Count * 100, 2);
      foreach (var tank in this.gasTanks) {
        this.oxygenCapacity += tank.Capacity;
        this.oxygenFillRatio += tank.Capacity * tank.FilledRatio;
        if (this.showOxygenTankStats && this.enableTerminalStatistics) {
          this.updateName (tank, true, "", tank.FilledRatio, 1);
        }
      }
      this.oxygenCapacityString = this.oxygenCapacity.Format ();
      this.oxygenFillRatioString = this.oxygenFillRatio.Format ();
    }
    void alignGyros() {
      if (this.gyros.Count == 0)
        return;
      if (this.shipControllers[0].IsUnderControl) {
        this.resetMotors ();
        this.currentActionString = "Automatic alignment paused.\n";
        this.currentActionString += "Ship is currently controlled by a player.";
        return;
      }
      int ǝ = 10;
      bool Ɂ = false;
      bool ɂ = false;
      bool Ƀ = false;
      string Ǹ = "";
      double Ȉ = this.maxSolarOutput;
      double Ȋ = this.maxDetectedOutput;
      double Ʒ = this.outputLast;
      double ǹ = maxGyroRPM - (maxGyroRPM - minGyroRPM) * (Ȉ / Ȋ);
      ǹ = ǹ / (Math.PI * 3);
      if (!Ȉ.IsBetween (this.ǡ - this.ǡ * this.realignPercentageGyro, this.ǡ + this.ǡ * this.realignPercentageGyro) && this.Ƙ && this.ƛ >= ǝ) {
        this.ƙ = false;
        this.ƚ = false;
        this.ǡ = 0;
        if (Ȉ < Ʒ && this.ƕ == 3 && !this.ǐ) { this.Ǥ = -this.Ǥ; this.ƕ = 0; this.ǐ = true; }
        this.Ǻ((float) (this.Ǥ * ǹ), 0, 0);
        if (this.Ǥ == -1) { Ǹ = "down"; } else { Ǹ = "up"; }
        if (Ȉ < Ʒ && this.ƕ >= 4) {
          this.stopGyros ();
          this.ƙ = true;
          this.ƚ = true;
          this.ǡ = Ȉ;
          this.ǐ = false;
          this.ƕ = 0;
          this.ƛ = 0;
        } else { Ɂ = true; this.ƕ++; }
      } else if (this.Ƙ) { this.stopGyros (); this.ƙ = true; this.ƚ = true; this.ǐ = false; this.ƕ = 0; this.ƛ++; } else { this.ƛ++; }
      if (!Ȉ.IsBetween (this.ǣ - this.ǣ * this.realignPercentageGyro, this.ǣ + this.ǣ * this.realignPercentageGyro) && this.ƚ && this.Ɲ >= ǝ) {
        this.Ƙ = false;
        this.ƙ = false;
        this.ǣ = 0;
        if (Ȉ < Ʒ && this.Ɨ == 3 && !this.Ɣ) { this.Ǒ = -this.Ǒ; this.Ɨ = 0; this.Ɣ = true; }
        this.Ǻ(0, 0, (float) (this.Ǒ * ǹ));
        if (this.Ǒ == -1) { Ǹ = "left"; } else { Ǹ = "right"; }
        if (Ȉ < Ʒ && this.Ɨ >= 4) {
          this.stopGyros ();
          this.Ƙ = true;
          this.ƙ = true;
          this.ǣ = Ȉ;
          this.Ɣ = false;
          this.Ɨ = 0;
          this.Ɲ = 0;
        } else {
          Ƀ = true;
          this.Ɨ++;
        }
      } else if (this.ƚ) {
        this.stopGyros ();
        this.Ƙ = true;
        this.ƙ = true;
        this.Ɣ = false;
        this.Ɨ = 0;
        this.Ɲ++;
      } else { this.Ɲ++; }
      if (!Ȉ.IsBetween (this.Ǣ - this.Ǣ * this.realignPercentageGyro, this.Ǣ + this.Ǣ * this.realignPercentageGyro) && this.ƙ && this.Ɯ >= ǝ) {
        this.Ƙ = false;
        this.ƚ = false;
        this.Ǣ = 0;
        if (Ȉ < Ʒ && this.Ɩ == 3 && !this.Ƒ) { this.Ǧ = -this.Ǧ; this.Ɩ = 0; this.Ƒ = true; }
        this.Ǻ(0, (float) (this.Ǧ * ǹ), 0);
        if (this.Ǧ == -1) { Ǹ = "left"; } else { Ǹ = "right"; }
        if (Ȉ < Ʒ && this.Ɩ >= 4) {
          this.stopGyros ();
          this.Ƙ = true;
          this.ƚ = true;
          this.Ǣ = Ȉ;
          this.Ƒ = false;
          this.Ɩ = 0;
          this.Ɯ = 0;
        } else { ɂ = true; this.Ɩ++; }
      } else if (this.ƙ) { this.stopGyros (); this.Ƙ = true; this.ƚ = true; this.Ƒ = false; this.Ɩ = 0; this.Ɯ++; } else { this.Ɯ++; }
      if (!Ɂ && !ɂ && !Ƀ) {
        this.currentActionString = "Aligned.";
      } else if (Ɂ) {
        this.currentActionString = "Aligning by pitching the ship " + Ǹ + "..";
      } else if (ɂ) {
        this.currentActionString = "Aligning by yawing the ship " + Ǹ + "..";
      } else if (Ƀ) {
        this.currentActionString = "Aligning by rolling the ship " + Ǹ + "..";
      }
    }
    void alignAllRotors() {
      if (this.maxSolarOutput < this.maxDetectedOutput * this.nightPercentage && this.ǜ >= 30) {
        this.currentActionString = "Night Mode.";
        this.isNight = true;
        if (this.rotateToSunrise && !this.ǚ) {
          if (this.manualAngle) {
            this.ǚ = this.rotateRotors("both", this.manualAngleHorizontal, this.manualAngleVertical);
          } else {
            this.ǚ = this.rotateRotors("sunrise");
          }
          if (this.ǚ) {
            foreach (var Ă in this.allRotors) { this.saveValue (Ă, "firstLockOfDay", 1); this.saveValue (Ă, "rotationDone", 0); }
          }
        } else { this.resetMotors (); }
        return;
      }
      if (this.isNight) {
        this.isNight = false;
        this.ǜ = 0;
        foreach (var rotor in this.allRotors) {
          this.saveValue (rotor, "outputMaxDayBefore", this.getDoubleValue (rotor, "outputMax"));
          this.saveValue (rotor, "outputMax", 0);
        }
      } else if (this.ǜ > 172800) {
        this.ǜ = 0;
      } else {
        this.ǜ++;
      }
      this.ǚ = false;
      this.shouldResetMotors = true;
      this.restPeriod = this.maxSolarOutput < this.maxDetectedOutput * 0.5 ? 30 : 10;
      int movingVerticalRotors = this.alignRotors(this.verticalRotors, true);
      int movingHorizontalRotors = this.alignRotors(this.horizontalRotors);
      if (movingVerticalRotors == 0 && movingHorizontalRotors == 0) {
        this.currentActionString = "Aligned.";
      } else if (movingVerticalRotors == 0) {
        this.currentActionString = "Aligning " + movingHorizontalRotors + " horizontal rotors..";
      } else if (movingHorizontalRotors == 0) {
        this.currentActionString = "Aligning " + movingVerticalRotors + " vertical rotors..";
      } else {
        this.currentActionString = "Aligning " + movingHorizontalRotors + " horizontal and " + movingVerticalRotors + " vertical rotors..";
      }
    }
    int alignRotors(List<IMyMotorStator> rotors, bool vertical = false) {
      int res = 0;
      foreach (var rotor in rotors) {
        double output = this.getDoubleValue (rotor, "output");
        double outputLast = this.getDoubleValue (rotor, "outputLast");
        double outputLocked = this.getDoubleValue (rotor, "outputLocked");
        double outputMax = this.getDoubleValue (rotor, "outputMax");
        double direction = this.getDoubleValue (rotor, "direction");
        double directionChanged = this.getDoubleValue (rotor, "directionChanged");
        double directionTimer = this.getDoubleValue (rotor, "directionTimer");
        double allowRotation = this.getDoubleValue (rotor, "allowRotation");
        double timeSinceRotation = this.getDoubleValue (rotor, "timeSinceRotation");
        bool reachedEnd = false;
        if (allowRotation == 0 || timeSinceRotation < this.restPeriod) {
          this.stopRotor (rotor);
          this.saveValue (rotor, "allowRotation", 1);
          this.saveValue (rotor, "timeSinceRotation", timeSinceRotation + 1);
          continue;
        }
        if (!output.IsBetween (outputLocked - outputLocked * this.realginPercentageRotor, outputLocked + outputLocked * this.realginPercentageRotor)) {
          // Avoids moving both directions at the same time
          if (vertical) {
            this.updateAllowRotationVerticalRotor(rotor, false);
          } else {
            this.updateAllowRotationHorizontalRotor(rotor, false);
          }
          outputLocked = 0;
          // reverse direction in some cases ? 
          if (output < outputLast && directionTimer == 2 && directionChanged == 0) {
            direction = -direction; directionTimer = 0; directionChanged = 1;
          }
          if ((rotor.LowerLimitDeg != float.MinValue || rotor.UpperLimitDeg != float.MaxValue) && directionTimer >= 5) {
            double currentAngle = this.getAngleDegrees (rotor);
            float minAngle = (float) Math.Round (rotor.LowerLimitDeg);
            float maxAngle = (float) Math.Round (rotor.UpperLimitDeg);
            if (currentAngle == minAngle || currentAngle == 360 + minAngle || currentAngle == maxAngle || currentAngle == 360 + maxAngle) {
              if (output < outputLast && directionChanged == 0) {
                direction = -direction;
                directionTimer = 0;
                directionChanged = 1;
              } else {
                reachedEnd = true;
              }
            }
          }
          // Slow down when near max output
          bool isNearMaxOutput = output.IsBetween (outputMax * 0.998, outputMax * 1.002);
          float rotSpeed = (float) (rotorMaxSpeed - rotorMaxSpeed * output.Ratio (outputMax) + rotorMinSpeed);
          if (!isNearMaxOutput)
            rotSpeed += rotorMinSpeed;
          this.setRotation(rotor, direction, rotSpeed);
          if ((output < outputLast && !isNearMaxOutput && directionTimer >= 3) || output == 0 || reachedEnd) {
            this.stopRotor (rotor);
            if (this.getDoubleValue (rotor, "firstLockOfDay") == 1) {
              if (output > this.getDoubleValue (rotor, "outputMaxDayBefore") * 0.9) {
                this.saveValue (rotor, "firstLockOfDay", 0);
                this.saveValue (rotor, "sunriseAngle", this.getAngleDegrees (rotor));
              }
            }
            outputLocked = output;
            directionChanged = 0;
            directionTimer = 0;
            timeSinceRotation = 0;
          } else {
            res++;
            directionTimer++;
          }
          this.saveValue (rotor,"outputLocked", outputLocked);
          this.saveValue (rotor, "direction", direction);
          this.saveValue (rotor, "directionChanged", directionChanged);
          this.saveValue (rotor, "directionTimer", directionTimer);
          this.saveValue (rotor, "timeSinceRotation", timeSinceRotation);
        } else {
          this.stopRotor (rotor);
        }
      }
      return res;
    }
    void updateAllowRotationHorizontalRotor(IMyMotorStator rotor, bool move) {
      foreach (var vRotor in this.verticalRotors) {
        if (rotor.CubeGrid == vRotor.TopGrid) {
          if (move) {
            this.saveValue (vRotor, "allowRotation", 1);
          } else {
            this.stopRotor (vRotor);
            this.saveValue (vRotor, "allowRotation", 0);
          }
        }
      }
    }
    void updateAllowRotationVerticalRotor(IMyMotorStator rotor, bool move) {
      foreach (var hRotor in this.horizontalRotors) {
        if (rotor.TopGrid == hRotor.CubeGrid) {
          if (move) {
            this.saveValue (hRotor, "allowRotation", 1);
          } else {
            this.stopRotor (hRotor);
            this.saveValue (hRotor, "allowRotation", 0);
          }
        }
      }
    }
    void setRotation(IMyMotorStator rotor, double factor, float speed = rotorMinSpeed) {
      rotor.RotorLock = false;
      rotor.TargetVelocityRPM = speed * (float) factor;
    }
    void Ǻ(double ǻ, double Ǽ, double ǽ) {
      Vector3D ǿ = new Vector3D (-ǻ, Ǽ, ǽ);
      Vector3D Ȇ = Vector3D.TransformNormal (ǿ, this.shipControllers[
        0].WorldMatrix);
      foreach (var Ȁ in this.gyros) {
        Vector3D ȁ = Vector3D.TransformNormal (Ȇ, Matrix.Transpose (Ȁ.WorldMatrix));
        Ȁ.GyroOverride = true;
        Ȁ.GyroPower = gyroPower;
        Ȁ.Pitch = (float) ȁ.X;
        Ȁ.Yaw = (float) ȁ.Y;
        Ȁ.Roll = (float) ȁ.Z;
      }
    }
    void stopRotor (IMyMotorStator rotor, bool lockRotor = true) {
      rotor.TargetVelocityRPM = 0f;
      if (lockRotor) {
        this.saveValue (rotor, "rotationDone", 1);
      } else {
        this.saveValue (rotor, "rotationDone", 0);
      }
      if (this.setRotorLockWhenStopped) {
        rotor.RotorLock = true;
      }
    }
    void stopGyros (bool stopOverride = false) {
      foreach (var gyro in this.gyros) {
        gyro.Pitch = 0;
        gyro.Yaw = 0;
        gyro.Roll = 0;
        if (stopOverride) gyro.GyroOverride = false;
      }
    }
    void resetMotors (bool ȃ = true) {
      foreach (var r in this.allRotors) {
        this.stopRotor (r, ȃ);
        this.saveValue (r, "timeSinceRotation", 0);
      }
      this.stopGyros (true);
      this.ƛ = 0;
      this.Ɯ = 0;
      this.Ɲ = 0;
    }
    bool isAtTarget(IMyMotorStator rotor, double targetAngle, bool isOffset = true) {
      double currentAngle = this.getAngleDegrees (rotor);
      bool reverse = false;
      if (isOffset) {
        if (rotor.CustomName.IndexOf ("[90]") >= 0) {
          targetAngle += 90;
        } else if (rotor.CustomName.IndexOf ("[180]") >= 0) {
          targetAngle += 180;
        } else if (rotor.CustomName.IndexOf ("[270]") >= 0) {
          targetAngle += 270;
        }
        if (targetAngle >= 360)
          targetAngle -= 360;
        if (rotor.Orientation.Up.ToString () == "Down") {
          reverse = true;
        } else if (rotor.Orientation.Up.ToString () == "Backward") {
          reverse = true;
        } else if (rotor.Orientation.Up.ToString () == "Left") {
          reverse = true;
        }
      }
      if (rotor.LowerLimitDeg != float.MinValue || rotor.UpperLimitDeg != float.MaxValue) {
        if (reverse)
          targetAngle = -targetAngle;
        if (targetAngle > rotor.UpperLimitDeg) {
          targetAngle = Math.Floor (rotor.UpperLimitDeg);
        }
        if (targetAngle < rotor.LowerLimitDeg) {
          targetAngle = Math.Ceiling (rotor.LowerLimitDeg);
        }
      } else {
        if (reverse) targetAngle = 360 - targetAngle;
      }
      if (currentAngle.IsBetween (targetAngle - 1, targetAngle + 1) || currentAngle.IsBetween (360 + targetAngle - 1, 360 + targetAngle + 1)) {
        this.stopRotor (rotor);
        return true;
      } else {
        int delta = currentAngle < targetAngle ? 1 : -1;
        if (currentAngle <= 90 && targetAngle >= 270) {
          delta = -1;
        }
        if (currentAngle >= 270 && targetAngle <= 90) {
          delta = 1;
        }
        float speed = Math.Abs (currentAngle - targetAngle) > 15 ? 1f : 0.2f;
        if (Math.Abs (currentAngle - targetAngle) < 3)
          speed = 0.1f;
        this.setRotation(rotor, delta, speed);
        return false;
      }
    }
    bool rotateRotors(string param, double horizontalAngle = 0, double verticalAngle = 0) {
      bool allAligned = true;
      int nVerticalRotors = 0;
      int nHorizontalRotors = 0;
      if (this.shouldResetMotors) {
        this.shouldResetMotors = false;
        this.resetMotors (false);
      }
      if (param !="verticalOnly") {
        foreach (var rotor in this.horizontalRotors) {
          if (this.getDoubleValue (rotor, "rotationDone") == 1)
            continue;
          bool isOffset = true;
          double targetAngle = horizontalAngle;
          if (param == "sunrise") {
            targetAngle = this.getDoubleValue (rotor, "sunriseAngle");
            isOffset = false;
          }
          if (!this.isAtTarget(rotor, targetAngle, isOffset)) {
            allAligned = false;
            nHorizontalRotors++;
            this.currentStatusString = nHorizontalRotors + " horizontal rotors are set to " + horizontalAngle + "°";
            if (param == "sunrise")
              this.currentStatusString = nHorizontalRotors + " horizontal rotors are set to sunrise position";
          }
        }
      }
      if (!allAligned)
        return false;
      if (param != "horizontalOnly") {
        foreach (var rotor in this.verticalRotors) {
          if (this.getDoubleValue (rotor, "rotationDone") == 1)
            continue;
          bool isOffset = true;
          double targetAngle = verticalAngle;
          if (param == "sunrise") {
            targetAngle = this.getDoubleValue (rotor, "sunriseAngle");
            isOffset = false;
          }
          if (!this.isAtTarget(rotor, targetAngle, isOffset)) {
            allAligned = false;
            nVerticalRotors++;
            this.currentStatusString = nVerticalRotors + " vertical rotors are set to " + verticalAngle + "°";
            if (param == "sunrise")
              this.currentStatusString = nVerticalRotors + " vertical rotors are set to sunrise position";
          }
        }
      }
      if (allAligned)
        this.shouldResetMotors = true;
      return allAligned;
    }
    void ȝ() {
      int Ȗ = 0;
      int ș = 0;
      if (this.ǥ ==
        90) {
        foreach (var Ă in this.allRotors) {
          this.stopRotor (Ă, false);
          double ȗ = 1;
          if (Ă.Orientation.Up.ToString () == "Up") { ȗ = -1; } else if (Ă.Orientation.Up.ToString () == "Forward") { ȗ = -1; } else if (Ă.Orientation.Up.ToString () == "Right") { ȗ = -1; }
          this.saveValue (Ă, "outputMax", this.getDoubleValue (Ă, "output"));
          this.saveValue (Ă, "direction",
            ȗ);
          this.saveValue (Ă, "directionChanged", 0);
          this.saveValue (Ă, "directionTimer", 0);
          this.maxDetectedOutput = 0;
        }
      }
      foreach (var Ǿ in this.horizontalRotors) {
        if (this.getDoubleValue (Ǿ, "rotationDone") == 1)
          continue;
        double
        Ȉ = this.getDoubleValue (Ǿ, "output");
        double Ʒ = this.getDoubleValue (Ǿ, "outputLast");
        double Ȋ = this.getDoubleValue (Ǿ, "outputMax");
        double Ș = this.getDoubleValue (Ǿ, "outputMaxAngle");
        double Ǹ = this.getDoubleValue (Ǿ,
          "direction");
        double ż = this.getDoubleValue (Ǿ, "directionChanged");
        double õ = this.getDoubleValue (Ǿ, "directionTimer");
        if (Ȋ == 0)
          Ȋ = 1;
        if (ż != 2) {
          Ȗ++;
          if (Ȉ < Ʒ && õ >= 7 && ż == 0) {
            this.saveValue (Ǿ,
              "direction", -Ǹ);
            this.saveValue (Ǿ, "directionChanged", 1);
            õ = 0;
          }
          if ((Ǿ.LowerLimitDeg != float.MinValue || Ǿ.UpperLimitDeg != float.MaxValue) && õ >= 3 && ż == 0) {
            double Ț = this.getAngleDegrees (Ǿ);
            float ț = (float) Math.Round (Ǿ.LowerLimitDeg);
            float Ȝ = (float) Math.Round (Ǿ.UpperLimitDeg);
            if (Ț == ț || Ț == 360 + ț || Ț == Ȝ || Ț ==
              360 + Ȝ) { this.saveValue (Ǿ, "direction", -Ǹ); this.saveValue (Ǿ, "directionChanged", 1); õ = 0; }
          }
          this.setRotation(Ǿ, Ǹ, (float) (2.75 - Ȉ.Ratio (Ȋ) * 2));
          if (Ȉ < Ʒ && õ >= 7 && ż == 1) {
            this.stopRotor (Ǿ, false);
            this.saveValue (Ǿ, "directionChanged", 2);
          } else { this.saveValue (Ǿ, "directionTimer", õ + 1); }
        } else { if (!this.isAtTarget(Ǿ, Ș, false)) Ȗ++; }
      }
      if (Ȗ != 0)
        return;
      foreach (var ȓ in this.verticalRotors) {
        if (this.getDoubleValue (ȓ, "rotationDone") == 1)
          continue;
        double Ȉ = this.getDoubleValue (ȓ, "output");
        double Ʒ = this.getDoubleValue (ȓ, "outputLast");
        double Ȋ = this.getDoubleValue (ȓ, "outputMax");
        double Ș
          = this.getDoubleValue (ȓ, "outputMaxAngle");
        double Ǹ = this.getDoubleValue (ȓ, "direction");
        double ż = this.getDoubleValue (ȓ, "directionChanged");
        double õ = this.getDoubleValue (ȓ, "directionTimer");
        if (Ȋ == 0)
          Ȋ = 1;
        if (ż != 2) {
          ș++;
          if (Ȉ < Ʒ && õ >= 7 && ż == 0) { this.saveValue (ȓ, "direction", -Ǹ); this.saveValue (ȓ, "directionChanged", 1); õ = 0; }
          if ((ȓ.LowerLimitDeg != float.MinValue || ȓ.UpperLimitDeg != float.MaxValue) && õ >= 3 && ż == 0) {
            double Ñ = this.getAngleDegrees (ȓ);
            float ö = (float) Math.Round (ȓ.LowerLimitDeg);
            float ø = (float)
            Math.Round (ȓ.UpperLimitDeg);
            if (Ñ == ö || Ñ == 360 + ö || Ñ == ø || Ñ == 360 + ø) { this.saveValue (ȓ, "direction", -Ǹ); this.saveValue (ȓ, "directionChanged", 1); õ = 0; }
          }
          this.setRotation(ȓ, Ǹ, (
            float) (2.75 - Ȉ.Ratio (Ȋ) * 2));
          if (Ȉ < Ʒ && õ >= 7 && ż == 1) { this.stopRotor (ȓ, false); this.saveValue (ȓ, "directionChanged", 2); } else { this.saveValue (ȓ, "directionTimer", õ + 1); }
        } else {
          if (!this.isAtTarget(
              ȓ, Ș, false))
            ș++;
        }
      }
      if (Ȗ == 0 && ș == 0) { this.ǥ = 0; }
    }
    void updateName (IMyTerminalBlock block, bool addPercent = true, string label = "", double ý = 0, double þ = 0) {
      string name = block.CustomName;
      string currentPercent = System.Text.RegularExpressions.Regex.Match (block.CustomName, @" *\(\d+\.*\d*%.*\)").Value;
      if (currentPercent != String.Empty) {
        name = block.CustomName.Replace (currentPercent, "");
      }
      if (addPercent) {
        name += " (" + ý.FormatPercent (þ);
        if (label != "") {
          name += ", " + label;
        }
        name += ")";
      }
      if (name != block.CustomName) {
        block.CustomName = name;
      }
    }
    double getAngleDegrees (IMyMotorStator rotor) {
      return Math.Round (rotor.Angle * 180 / Math.PI);
    }
    StringBuilder ă(IMyTextSurface Æ, bool W = true, bool Ą = true,
      bool ą = true, bool ï = true, bool ó = true, bool ð = true, bool ñ = true, bool ç = true) {
      bool î = false;
      StringBuilder Y = new StringBuilder ();
      if (W) { Y.Append ("Isy's Solar Alignment " + this.doodleSequence[this.doodleCount] + "\n"); Y.Append (Æ.Ģ('=', Æ.Ŭ(Y))).Append ("\n\n"); }
      if (Ą && this.warning != null) {
        Y.Append (
          "Warning!\n" + this.warning + "\n\n");
        î = true;
      }
      if (ą) { string è = this.currentActionString + "\n" + this.statusString; Y.Append (è); Y.Append ('\n'.Repeat (3 - è.Count (é => é == '\n'))); î = true; }
      if (ï) {
        Y.Append (
          "Statistics for " + this.solarPanels.Count + " Solar Panels:\n");
        Y.Append (this.Ú(Æ, "Efficiency", this.maxSolarOutput, this.maxDetectedOutput, this.maxSolarOutputString, this.maxDetectedOutputString));
        Y.Append (this.Ú(Æ, "Output", this.currentSolarOutput, this.maxSolarOutput, this.currentSolarOutputString, this.maxSolarOutputString) + "\n\n");
        î = true;
      }
      if (ó && this.windTurbines.Count > 0) { Y.Append ("Statistics for " + this.windTurbines.Count + " Wind Turbines:\n"); Y.Append (this.Ú(Æ, "Output", this.windCurrentOutput, this.windMaxOutput, this.windCurrentOutputString, this.windMaxOutputString) + "\n\n"); î = true; }
      if (ð && this.batteries.Count >
        0) {
        Y.Append ("Statistics for " + this.batteries.Count + " Batteries:\n");
        Y.Append (this.Ú(Æ, "Input", this.batteryCurrentInput, this.batteryMaxInput, this.batteryCurrentInputString, this.batteryMaxInputString));
        Y.Append (this.Ú(Æ, "Output", this.batteryCurrentOutput, this.batteryMaxOutput, this.batteryCurrentOutputString, this.batteryMaxOutputString));
        Y.
        Append (this.Ú(Æ, "Charge", this.batteryCurrentStoredPower, this.batteryMaxStoredPower, this.batteryCurrentStoredPowerString, this.batteryMaxStoredPowerString) + "\n\n");
        î = true;
      }
      if (ñ && (this.oxygenFarms.Count > 0 || this.gasTanks.Count > 0)) {
        Y.Append ("Statistics for Oxygen:\n");
        if (this.oxygenFarms.Count > 0) {
          Y.Append (this.Ú(Æ, this.oxygenFarms.Count + " Farms", this.oxygenOutput, 100));
        }
        if (this.gasTanks.Count > 0) { Y.Append (this.Ú(Æ, this.gasTanks.Count + " Tanks", this.oxygenFillRatio, this.oxygenCapacity, this.oxygenFillRatioString, this.oxygenCapacityString)); }
        Y.Append ("\n\n");
        î = true;
      }
      if (ç && !this.useGyroMode) {
        string ê = "";
        string ë = "";
        string ì = "";
        if (this.dayLength < this.dayTimer) { ë = " inaccurate"; ê = "*"; } else if (this.dayLength == defaultDayLength || this.sunSet == defaultDaytimeLength) {
          ë =
            " inaccurate, still calculating";
          ê = "*";
        }
        if (this.dayTimer < this.sunSet && ê == "") { ì = " / Dusk in: " + this.y (this.sunSet - this.dayTimer); } else if (this.dayTimer > this.sunSet && ê == "") { ì = " / Dawn in: " + this.y (this.dayLength - this.dayTimer); }
        Y.Append (
          "Time of your location:\n");
        Y.Append ("Time: " + this.e (this.dayTimer) + ì + ê + "\n");
        Y.Append ("Dawn: " + this.e (this.dayLength) + " / Daylength: " + this.y (this.sunSet) + ê + "\n");
        Y.Append ("Dusk: " + this.e (this.sunSet) +
          " / Nightlength: " + this.y (this.dayLength - this.sunSet) + ê + "\n");
        if (ê != "") { Y.Append (ê + ë); }
        î = true;
      }
      if (!î) { Y.Append ("-- No informations to show --"); }
      return Y;
    }
    StringBuilder í(IMyTextSurface Æ, bool ï = false, bool ó = false, bool ð = false, bool ñ = false, bool ç = false, bool ò = false) {
      bool î = false;
      StringBuilder Y = new StringBuilder ();
      if (ï) {
        Y.Append ("Statistics for " + this.solarPanels.Count + " Solar Panels:\n");
        Y.Append (this.Ú(Æ, "Efficiency", this.maxSolarOutput, this.maxDetectedOutput, this.maxSolarOutputString, this.maxDetectedOutputString, á:
          true));
        Y.Append (this.Ú(Æ, "Output", this.currentSolarOutput, this.maxSolarOutput, this.currentSolarOutputString, this.maxSolarOutputString, á: true));
        î = true;
      }
      if (ó && this.windTurbines.Count > 0) {
        if (î)
          Y.Append ("\n");
        Y.Append ("Statistics for " + this.windTurbines.Count + " Wind Turbines:\n");
        Y.Append (this.Ú(Æ, "Output", this.windCurrentOutput, this.windMaxOutput, this.windCurrentOutputString, this.windMaxOutputString, á: true));
        î = true;
      }
      if (ð && this.batteries.Count > 0) {
        if (î)
          Y.Append ("\n");
        Y.Append (
          "Statistics for " + this.batteries.Count + " Batteries:\n");
        Y.Append (this.Ú(Æ, "Input", this.batteryCurrentInput, this.batteryMaxInput, this.batteryCurrentInputString, this.batteryMaxInputString, á: true));
        Y.Append (this.Ú(Æ, "Output", this.batteryCurrentOutput, this.batteryMaxOutput, this.batteryCurrentOutputString, this.batteryMaxOutputString, á: true));
        Y.Append (this.Ú(Æ,
          "Charge", this.batteryCurrentStoredPower, this.batteryMaxStoredPower, this.batteryCurrentStoredPowerString, this.batteryMaxStoredPowerString, á: true));
        î = true;
      }
      if (ñ && (this.oxygenFarms.Count > 0 || this.gasTanks.Count > 0)) {
        if (î)
          Y.Append ("\n");
        Y.Append ("Statistics for Oxygen:\n");
        if (this.oxygenFarms.Count > 0) { Y.Append (this.Ú(Æ, this.oxygenFarms.Count + " Farms", this.oxygenOutput, 100, á: true)); }
        if (this.gasTanks.Count > 0) { Y.Append (this.Ú(Æ, this.gasTanks.Count + " Tanks", this.oxygenFillRatio, this.oxygenCapacity, this.oxygenFillRatioString, this.oxygenCapacityString, á: true)); }
        î = true;
      }
      if (ç) {
        if (î)
          Y.Append ("\n");
        if (this.useGyroMode) { Y.Append ("Location time is not available in gyro mode!"); } else {
          string q = this.e (this.dayTimer);
          Y.
          Append (Æ.Ģ(' ', (Æ.ř() - Æ.Ŭ(q)) / 2)).Append (q + "\n");
        }
        î = true;
      }
      if (ò) {
        if (î)
          Y.Append ("\n");
        string q = DateTime.Now.ToString (@"HH:mm:ss");
        Y.Append (Æ.Ģ(' ', (Æ.ř() - Æ.Ŭ(q)) / 2)).Append (q + "\n");
        î = true;
      }
      if (!î) {
        Y.Append (
          "Edit the custom data and set,\nwhat should be shown here!");
      }
      return Y;
    }
    void updateMainDisplays (string z = null) {
      if (this.mainLCDs.Count == 0) {
        this.displayCounter++;
        return;
      }
      for (int O = this.Ǔ; O < this.mainLCDs.Count; O++) {
        if (this.Ŕ())
          return;
        this.Ǔ++;
        var Č = this.mainLCDs[O].ė(this.mainLcdKeyword);
        foreach (var ô in Č) {
          var æ = ô.Key;
          var T = ô.Value;
          if (!æ.GetText ().EndsWith ("\a")) {
            æ.Font = this.defaultFont;
            æ.FontSize = this.defaultFontSize;
            æ.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            æ.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
          }
          bool W = T.GetBool ("showHeading");
          bool Ą = T.GetBool ("showWarnings");
          bool ą = T.GetBool ("showCurrentOperation");
          bool ï = T.GetBool ("showSolarStats");
          bool ó = T.GetBool ("showTurbineStats");
          bool ð = T.GetBool ("showBatteryStats");
          bool ñ = T.GetBool ("showOxygenStats");
          bool ç = T.GetBool ("showLocationTime");
          bool X = T.GetBool ("scrollTextIfNeeded");
          StringBuilder Y = new StringBuilder ();
          if (z != null) {
            Y.Append (z);
          } else {
            Y = this.ă(æ, W, Ą, ą, ï, ó, ð, ñ, ç);
          }
          Y = æ.Ĩ(Y, W ? 3 : 0, X);
          æ.WriteText (Y.Append ("\a"));
        }
      }
      this.displayCounter++;
      this.Ǔ = 0;
    }
    void updateCompactDisplays () {
      if (this.compactLCDs.Count == 0) { this.displayCounter++; return; }
      for (int O = this.ǔ; O < this.compactLCDs.Count; O++) {
        if (
          this.Ŕ())
          return;
        this.ǔ++;
        var Č = this.compactLCDs[O].ė(this.compactLcdKeyword);
        foreach (var ô in Č) {
          var æ = ô.Key;
          var T = ô.Value;
          if (!æ.GetText ().EndsWith (
              "\a")) {
            æ.Font = this.defaultFont;
            æ.FontSize = this.defaultFontSize;
            æ.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            æ.ContentType =
              VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
          }
          bool ï = T.GetBool ("showSolarStats");
          bool ó = T.GetBool ("showTurbineStats");
          bool ð = T.GetBool (
            "showBatteryStats");
          bool ñ = T.GetBool ("showOxygenStats");
          bool ç = T.GetBool ("showLocationTime");
          bool ò = T.GetBool ("showRealTime");
          bool X = T.GetBool (
            "scrollTextIfNeeded");
          StringBuilder Y = new StringBuilder ();
          Y = this.í(æ, ï, ó, ð, ñ, ç, ò);
          Y = æ.Ĩ(Y, 0, X);
          æ.WriteText (Y.Append ("\a"));
        }
      }
      this.displayCounter++;
      this.ǔ = 0;
    }
    void updateWarningDisplays () {
      if (this.warningLCDs.Count == 0) { this.displayCounter++; return; }
      StringBuilder Ĉ = new StringBuilder ();
      if (this.previousWarnings.Count == 0) { Ĉ.Append ("- No problems detected -"); } else {
        int ĉ = 1;
        foreach (var Ċ in this.previousWarnings) { Ĉ.Append (ĉ + ". " + Ċ.Replace ("\n", " ") + "\n"); ĉ++; }
      }
      for (int O = this.Ǖ; O < this.warningLCDs.Count; O++) {
        if (this.Ŕ())
          return;
        this.Ǖ++;
        var Č = this.warningLCDs[O].ė(this.warningsLcdKeyword);
        foreach (var ô in Č) {
          var æ = ô.Key;
          var T = ô.Value;
          if (!æ.GetText ().EndsWith ("\a")) {
            æ.Font = this.defaultFont;
            æ.FontSize = this.defaultFontSize;
            æ.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            æ.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
          }
          bool W = T.GetBool ("showHeading");
          bool X = T.GetBool ("scrollTextIfNeeded");
          StringBuilder Y = new StringBuilder ();
          if (W) {
            Y.Append ("Isy's Solar Alignment Warnings\n");
            Y.Append (æ.Ģ('=', æ.Ŭ(Y))).Append ("\n\n");
          }
          Y.Append (Ĉ);
          Y = æ.Ĩ(Y, W ? 3 : 0, X);
          æ.WriteText (Y.Append ("\a"));
        }
      }
      this.displayCounter++;
      this.Ǖ = 0;
    }
    void updatePerformanceDisplays () {
      if (this.performanceLCDs.Count == 0) { this.displayCounter++; return; }
      for (int O = this.ǖ; O < this.performanceLCDs.Count; O++) {
        if (this.Ŕ())
          return;
        this.ǖ++;
        var Č = this.performanceLCDs[O].ė(this.performanceLcdKeyword);
        foreach (var ô in Č) {
          var æ = ô.Key;
          var T = ô.Value;
          if (!æ.GetText ().EndsWith ("\a")) {
            æ.
            Font = this.defaultFont;
            æ.FontSize = this.defaultFontSize;
            æ.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            æ.ContentType = VRage.Game
              .GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
          }
          bool W = T.GetBool ("showHeading");
          bool X = T.GetBool ("scrollTextIfNeeded");
          StringBuilder Y = new
          StringBuilder ();
          if (W) { Y.Append ("Isy's Solar Alignment Performance\n"); Y.Append (æ.Ģ('=', æ.Ŭ(Y))).Append ("\n\n"); }
          Y.Append (this.performanceStringBuilder);
          Y = æ.Ĩ(Y, W ?
            3 : 0, X);
          æ.WriteText (Y.Append ("\a"));
        }
      }
      this.displayCounter++;
      this.ǖ = 0;
    }
    void echoDebug () {
      if (this.runCounter == 99) { this.runCounter = 0; } else { this.runCounter++; }
      this.Echo ("Isy's Solar Alignment " + this.doodleSequence[this.doodleCount] + "\n========================\n");
      if (this.warning != null) {
        this.Echo ("Warning!\n" + this.warning + "\n");
      }
      StringBuilder Y = new StringBuilder ();
      Y.Append ("Script is running in " + (this.useGyroMode ? "gyro" : "rotor") + " mode\n\n");
      Y.Append ("Task: " + this.stepNames[this.step] + "\n");
      Y.Append ("Script step: " + this.step + " / " + (this.stepNames.Length - 1) + "\n\n");
      this.performanceStringBuilder = Y.Append (this.performanceStringBuilder);
      Y.Append ("Main Grid: " + this.Ŧ.CustomName + "\n");
      if (this.attachedGrids.Count > 0)
        Y.Append ("Connected Grids: " + this.attachedGrids.Count + "\n");
      if (this.allRotors.Count > 0)
        Y.Append ("Rotors: " + this.allRotors.Count + "\n");
      if (this.gyros.Count > 0)
        Y.Append ("Gyros: " + this.gyros.Count + "\n");
      if (this.solarPanels.Count > 0)
        Y.Append ("Solar Panels: " + this.solarPanels.Count + "\n");
      if (this.windTurbines.Count > 0)
        Y.Append ("Wind Turbines: " + this.windTurbines.Count + "\n");
      if (this.oxygenFarms.Count > 0)
        Y.Append ("Oxygen Farms: " + this.oxygenFarms.Count + "\n");
      if (this.gasTanks.Count > 0)
        Y.Append ("Oxygen Tanks: " + this.gasTanks.Count + "\n");
      if (this.batteries.Count > 0)
        Y.Append ("Batteries: " + this.batteries.Count + "\n");
      if (this.reactorCount > 0)
        Y.Append ("Reactors: " + this.reactorCount + "\n");
      if (this.hydrogenEngineCount > 0)
        Y.Append ("Hydrogen Engines: " + this.hydrogenEngineCount + "\n");
      if (this.mainLCDs.Count > 0)
        Y.Append ("LCDs: " + this.mainLCDs.Count + "\n");
      if (this.displays.Count > 0)
        Y.Append ("Corner LCDs: " + this.displays.Count + "\n");
      if (this.lights.Count > 0)
        Y.Append ("Lights: " + this.lights.Count + "\n");
      if (this.spotlights.Count > 0)
        Y.Append ("Spotlights: " + this.spotlights.Count + "\n");
      if (this.timers.Length > 0)
        Y.Append ("Timer Blocks: " + this.timers.Length + "\n");
      this.Echo (this.performanceStringBuilder.ToString ());
      if (this.mainLCDs.Count == 0) {
        this.Echo ("Hint:\nBuild a LCD and add the main LCD\nkeyword '" + this.mainLcdKeyword + "' to its name to get\nmore informations about your base\nand the current script actions.\n");
      }
    }
    void timeRelatedStuff () {
      this.dayTimer += 1;
      this.ƽ += 1;
      if (this.dayTimer > 172800) { this.dayTimer = 0; this.ƽ = 0; }
      double d = this.maxDetectedOutput * this.nightTimePercentage;
      if (this.maxSolarOutput < d && this.outputLast >= d && this.ƽ > 300) { this.sunSet = this.dayTimer; this.ƽ = 0; }
      if (this.maxSolarOutput > d && this.outputLast <= d && this.ƽ > 300) {
        if (this.sunSet !=
          defaultDaytimeLength) { this.dayLength = this.dayTimer; }
        this.dayTimer = 0;
        this.ƽ = 0;
      }
      if (this.sunSet > this.dayLength) { this.dayLength = this.sunSet * 2; }
    }
    string e (double f, bool h = false) {
      string j = "";
      f = f % this.dayLength;
      double k = this.sunSet + (this.dayLength - this.sunSet) / 2D;
      double m = this.dayLength / 24D;
      double q;
      if (f < k) { q = (f + (this.dayLength - k)) / m; } else { q = (f - k) / m; }
      double u = Math.Floor (q);
      double v = Math.Floor ((q % 1 * 100) * 0.6);
      string w = u.ToString (
        "00");
      string x = v.ToString ("00");
      j = w + ":" + x;
      if (h) { return u.ToString (); } else { return j; }
    }
    string y (int N) {
      string U = "";
      TimeSpan A =
        TimeSpan.FromSeconds (N);
      U = A.ToString (@"hh\:mm\:ss");
      return U;
    }
    void handlePowerFallback () {
      if (this.fallbackPowerProducers.Count == 0)
        return;
      double B = this.turnOnAtPercent % 100 / 100;
      double C = this.turnOffAtPercent % 100 / 100;
      double D = this.overloadPercentage % 100 / 100;
      if (this.Ǌ == "lowBat" || this.Ǌ == "") {
        if (this.activateOnLowBattery && this.batteryCurrentStoredPower < this.batteryMaxStoredPower * B) {
          this.Ǆ = true;
          this.Ǌ = "lowBat";
        } else if (this.activateOnLowBattery && this.batteryCurrentStoredPower > this.batteryMaxStoredPower * C) { this.Ǆ = false; this.Ǌ = ""; }
      }
      if (this.Ǌ == "overload" || this.Ǌ == "") {
        if (this.activateOnOverload && this.batteryCurrentOutput + this.currentSolarOutput +
          this.windCurrentOutput > (this.batteryMaxOutput + this.maxSolarOutput + this.windMaxOutput) * D) { this.Ǆ = true; this.Ǌ = "overload"; } else { this.Ǆ = false; this.Ǌ = ""; }
      }
      if (this.batteryCurrentStoredPower < this.ǉ || (this.Ǆ && this.ǅ && this.ǆ)) { this.ǅ = true; this.ǆ = true; } else {
        if (
          this.activateHydrogenEngineFirst && this.hydrogenEngineCount > 0) { this.ǆ = true; this.ǅ = false; } else if (!this.activateHydrogenEngineFirst && this.reactorCount > 0) { this.ǆ = false; this.ǅ = true; } else { this.ǆ = true; this.ǅ = true; }
      }
      this.ǉ = this.batteryCurrentStoredPower;
      foreach (var E in this.fallbackPowerProducers) {
        if (this.Ǆ) {
          if (this.ǅ && E.BlockDefinition.TypeIdString.Contains ("Reactor")) { E.Enabled = true; } else if (this.ǆ && E.BlockDefinition.TypeIdString.Contains ("HydrogenEngine")) { E.Enabled = true; } else { E.Enabled = false; }
        } else { E.Enabled = false; }
      }
      if (this.Ǌ == "lowBat")
        this.currentStatusString =
        "Power fallback active: Low battery charge!";
      if (this.Ǌ == "overload")
        this.currentStatusString = "Power fallback active: Overload!";
    }
    void handleLights () {
      if (this.lights.Count == 0 && this.spotlights.Count == 0)
        return;
      int G = 0;
      int.TryParse (this.e (this.dayTimer, true), out G);
      bool H = true;
      if (!this.simpleMode) {
        if (this.dayTimer != this.dayLength && G >= this.lightOffHour && G < this.lightOnHour) { H = false; } else if (this.dayTimer == this.dayLength && this.maxSolarOutput > this.maxDetectedOutput *
          this.nightTimePercentage) { H = false; }
      } else { if (this.maxSolarOutput > this.maxDetectedOutput * (this.simpleThreshold % 100) / 100) H = false; }
      foreach (var I in this.lights) { I.Enabled = H; }
      foreach (var K in this.spotlights) {
        K.
        Enabled = H;
      }
    }
    void handleTimerBlock () {
      if (this.events.Length == 0) { this.log ("No events for triggering specified!"); } else if (this.timers.Length == 0) {
        this.log (
          "No timers for triggering specified!");
      } else if (this.events.Length != this.timers.Length) {
        this.log ("Every event needs a timer block name!\nFound " + this.events.Length + " events and " +
          this.timers.Length + " timers.");
      } else {
        int L = -1;
        string M = "";
        int N;
        for (int O = 0; O <= this.events.Length - 1; O++) {
          if (this.events[O] == "sunrise" && this.dayTimer == 0) {
            L = O;
            M = "sunrise";
          } else if (this.events[O] == "sunset" && this.dayTimer == this.sunSet) { L = O; M = "sunset"; } else if (int.TryParse (this.events[O], out N) == true && this.dayTimer % N == 0) {
            L = O;
            M = N + " seconds";
          } else if (this.e (this.dayTimer) == this.events[O]) { L = O; M = this.events[O]; }
        }
        foreach (var P in this.timers) {
          var Q = this.GridTerminalSystem.
          GetBlockWithName (P) as IMyTimerBlock;
          if (Q == null) { this.log ("External timer block not found:\n'" + Q.CustomName + "'"); } else {
            if (Q.GetOwnerFactionTag () !=
              this.Me.GetOwnerFactionTag ()) {
              this.log ("'" + Q.CustomName +
                "' has a different owner / faction!\nAll blocks should have the same owner / faction!");
            }
            if (Q.Enabled == false) { this.log ("'" + Q.CustomName + "' is turned off!\nTurn it on in order to be used by the script!"); }
          }
        }
        if (L >= 0) {
          var Q = this.GridTerminalSystem.GetBlockWithName (this.timers[L]) as IMyTimerBlock;
          if (Q != null) {
            Q.ApplyAction ("Start");
            this.currentActionString =
              "External timer triggered! Reason: " + M;
          }
        }
      }
    }
    void log (string z) { this.previousWarnings.Add (z); this.currentWarnings.Add (z); this.warning = this.previousWarnings.ElementAt (0); }
    void resetAll () {
      foreach (var panel in this.solarPanels) {
        panel.CustomData = "";
        this.updateName (panel, false);
      }
      foreach (var b in this.batteries) { this.updateName (b, false); }
      foreach (var f in this.oxygenFarms) { this.updateName (f, false); }
      foreach (var t in this.gasTanks) { this.updateName (t, false); }
    }
    void Deserialize () {
      if (this.Storage.Length > 0) {
        var lines = this.Storage.Split ('\n');
        foreach (var line in lines) {
          var kv = line.Split ('=');
          if (kv.Length != 2)
            continue;
          if (kv[0] == "dayTimer") {
            int.TryParse (kv[1], out this.dayTimer);
          } else if (kv[0] == "dayLength") {
            int.TryParse (kv[1], out this.dayLength);
          } else if (kv[0] == "sunSet") {
            int.TryParse (kv[1], out this.sunSet);
          } else if (kv[0] == "outputLast") {
            float.TryParse (kv[1], out this.outputLast);
          } else if (kv[0] == "maxDetectedOutput") {
            float.TryParse (kv[1], out this.maxDetectedOutput);
          } else if (kv[0] == "solarPanelsCount") {
            int.TryParse (kv[1], out this.solarPanelsCount);
          } else if (kv[0] == "oxygenFarmsCount") {
            int.TryParse (kv[1], out this.oxygenFarmsCount);
          } else if (kv[0] == "action") {
            this.action = kv[1];
          }
        }
        if (this.action == "paused")
          this.Ƴ = true;
      }
    }
    void Save () {
      string T = "";
      T += "dayTimer=" + this.dayTimer + "\n";
      T += "dayLength=" + this.dayLength + "\n";
      T += "sunSet=" + this.sunSet + "\n";
      T += "outputLast=" + this.outputLast + "\n";
      T += "maxDetectedOutput=" + this.maxDetectedOutput + "\n";
      T += "solarPanelsCount=" + this.solarPanels.Count + "\n";
      T += "oxygenFarmsCount=" + this.oxygenFarms.Count + "\n";
      T += "action=" + this.action;
      this.Storage = T;
    }
    StringBuilder Ú(IMyTextSurface Æ, string Û, double Ü, double Ý, string Þ = null, string ß = null, bool à = false, bool á = false) {
      string â = Ü.ToString ();
      string ã = Ý.ToString ();
      if (Þ != null) { â = Þ; }
      if (ß != null) { ã = ß; }
      float å = Æ.FontSize;
      float Ò = Æ.ř();
      char µ = ' ';
      float Ç = Æ.Ų(µ);
      StringBuilder º = new StringBuilder (" " + Ü.FormatPercent (Ý));
      º = Æ.Ģ(µ, Æ.Ŭ("99999.9%") - Æ.Ŭ(º)).Append (º);
      StringBuilder À = new StringBuilder (â + " / " + ã);
      StringBuilder Á = new StringBuilder ();
      StringBuilder Â = new StringBuilder ();
      StringBuilder Ã;
      double Ä = 0;
      if (Ý > 0)
        Ä = Ü / Ý >= 1 ? 1 : Ü / Ý;
      if (á && !à) {
        if (å <= 0.5 || (å <= 1 && Ò > 512)) {
          Á.Append (this.Å(Æ, Ò * 0.25f, Ä) + " " + Û);
          Ã = Æ.Ģ(µ, Ò * 0.75 - Æ.Ŭ(Á) - Æ.Ŭ(â + " /"));
          Á.Append (Ã).Append (À);
          Ã = Æ.Ģ(µ, Ò - Æ.Ŭ(Á) - Æ.Ŭ(º));
          Á.Append (Ã);
          Á.Append (º);
        } else {
          Á.Append (this.Å(Æ, Ò * 0.3f, Ä) + " " + Û);
          Ã = Æ.Ģ(µ, Ò - Æ.Ŭ(Á) - Æ.Ŭ(º));
          Á.Append (Ã);
          Á.Append (º);
        }
      } else {
        Á.Append (Û + " ");
        if (å <= 0.6 || (å <= 1 && Ò > 512)) {
          Ã = Æ.Ģ(µ, Ò * 0.5 - Æ.Ŭ(Á) - Æ.Ŭ(â + " /"));
          Á.Append (Ã).Append (À);
          Ã = Æ.Ģ(µ, Ò - Æ.Ŭ(Á) - Æ.Ŭ(º));
          Á.Append (Ã).Append (º);
          if (!à) {
            Â = this.Å(Æ, Ò, Ä).Append ("\n");
          }
        } else {
          Ã = Æ.Ģ(µ, Ò - Æ.Ŭ(Á) - Æ.Ŭ(À));
          Á.Append (Ã).Append (À);
          if (!à) {
            Â = this.Å(Æ, Ò - Æ.Ŭ(º), Ä);
            Â.Append (º).Append ("\n");
          }
        }
      }
      return Á.Append ("\n").Append (Â);
    }
    StringBuilder Å(IMyTextSurface s, float È, double Ä) {
      StringBuilder É, Ê;
      char Ë = '[';
      char Ì = ']';
      char Í = 'I';
      char Î = '.';
      float Ï = s.Ų(Ë);
      float Ð = s.Ų(Ì);
      float V = È - Ï - Ð;
      É = s.Ģ(Í, V * Ä);
      Ê = s.Ģ(Î, V - s.Ŭ(É));
      return new StringBuilder ().Append (Ë).Append (É).Append (Ê).Append (Ì);
    }
    StringBuilder performanceStringBuilder = new StringBuilder ("No performance Information available!");
    Dictionary<string, int> maxInstructionCountsPerAction = new Dictionary<string, int> ();
    List<int> instructionCounts = new List<int> (new int[100]);
    List<double> elapsedTimes = new List<double> (new double[100]);
    double maxInstructionCount, maxElapsedTime;
    int runCounter = 0;
    DateTime last;
    void fetchPerformances (string action, bool reset = false) {
      if (reset) {
        this.last = DateTime.Now;
        return;
      }
      this.runCounter = this.runCounter >= 99 ? 0 : this.runCounter + 1;
      int currentInstructionCount = this.Runtime.CurrentInstructionCount;
      if (currentInstructionCount > this.maxInstructionCount)
        this.maxInstructionCount = currentInstructionCount;
      this.instructionCounts[this.runCounter] = currentInstructionCount;
      double averageInstructionCount = this.instructionCounts.Sum () / this.instructionCounts.Count;
      this.performanceStringBuilder.Clear ();
      this.performanceStringBuilder.Append ("Instructions: " + currentInstructionCount + " / " + this.Runtime.MaxInstructionCount + "\n");
      this.performanceStringBuilder.Append ("Max. Instructions: " + this.maxInstructionCount + " / " + this.Runtime.MaxInstructionCount + "\n");
      this.performanceStringBuilder.Append ("Avg. Instructions: " + Math.Floor (averageInstructionCount) + " / " + this.Runtime.MaxInstructionCount + "\n\n");
      double current = (DateTime.Now - this.last).TotalMilliseconds;
      if (current > this.maxElapsedTime && this.maxInstructionCountsPerAction.ContainsKey (action))
        this.maxElapsedTime = current;
      this.elapsedTimes[this.runCounter] = current;
      double averageElapsedTime = this.elapsedTimes.Sum () / this.elapsedTimes.Count;
      this.performanceStringBuilder.Append ("Last runtime: " + Math.Round (current, 4) + " ms\n");
      this.performanceStringBuilder.Append ("Max. runtime: " + Math.Round (this.maxElapsedTime, 4) + " ms\n");
      this.performanceStringBuilder.Append ("Avg. runtime: " + Math.Round (averageElapsedTime, 4) + " ms\n\n");
      this.performanceStringBuilder.Append ("Instructions per Method:\n");
      this.maxInstructionCountsPerAction[action] = currentInstructionCount;
      foreach (var P in this.maxInstructionCountsPerAction.OrderByDescending (O => O.Value)) {
        this.performanceStringBuilder.Append ("- " + P.Key + ": " + P.Value + "\n");
      }
      this.performanceStringBuilder.Append ("\n");
    }
    IMyCubeGrid Ŧ = null;
    HashSet<IMyCubeGrid> ŧ = new HashSet<IMyCubeGrid> ();
    void Ũ(IMyCubeGrid ŏ) {
      this.ŧ.Add (ŏ);
      List<IMyMotorStator> ő = new List<IMyMotorStator> ();
      List<IMyPistonBase> Œ = new List<IMyPistonBase> ();
      this.GridTerminalSystem.GetBlocksOfType<IMyMotorStator> (ő, œ => œ.IsAttached && œ.TopGrid == ŏ && !this.ŧ.Contains (œ.CubeGrid));
      this.GridTerminalSystem.GetBlocksOfType<IMyPistonBase> (Œ, ŋ => ŋ.IsAttached && ŋ.TopGrid == ŏ && !this.ŧ.Contains (ŋ.CubeGrid));
      if (ő.Count == 0 && Œ.Count == 0) {
        this.Ŧ = ŏ;
        return;
      } else {
        foreach (var Ă in ő) {
          this.Ũ(Ă.CubeGrid);
        }
        foreach (var Ō in Œ) {
          this.Ũ(Ō.CubeGrid);
        }
      }
    }
    HashSet<IMyCubeGrid> attachedGrids = new HashSet<IMyCubeGrid> ();
    void initAttachedGrids (IMyCubeGrid grid, bool clear = false) {
      if (clear)
        this.attachedGrids.Clear ();
      this.attachedGrids.Add (grid);
      List<IMyMotorStator> rotors = new List<IMyMotorStator> ();
      List<IMyPistonBase> pistons = new List<IMyPistonBase> ();
      this.GridTerminalSystem.GetBlocksOfType<IMyMotorStator> (rotors, r => r.CubeGrid == grid && r.IsAttached && !this.attachedGrids.Contains (r.TopGrid));
      this.GridTerminalSystem.GetBlocksOfType<IMyPistonBase> (pistons, p => p.CubeGrid == grid && p.IsAttached && !this.attachedGrids.Contains (p.TopGrid));
      foreach (var r in rotors) { this.initAttachedGrids (r.TopGrid); }
      foreach (var p in pistons) { this.initAttachedGrids (p.TopGrid); }
    }
    bool Ŕ(double Ü = 10) {
      return this.Runtime.CurrentInstructionCount > Ü * 1000;
    }
    List<IMyTerminalBlock> findDisplayBlocks (string keyword, string[] defaultOptions = null) {
      string defaultKeyword = "[IsyLCD]";
      var blocks = new List<IMyTerminalBlock> ();
      this.GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider> (blocks,
        b => b.IsSameConstructAs (this.Me) && (b.CustomName.Contains (keyword) || (b.CustomName.Contains (defaultKeyword) && b.CustomData.Contains (keyword))));
      var blocksWithKeyword = blocks.FindAll (ũ => ũ.CustomName.Contains (keyword));
      foreach (var block in blocksWithKeyword) {
        block.CustomName = block.CustomName.Replace (keyword, "").Replace (" " + keyword, "").TrimEnd (' ');
        bool addDefaultKW = false;
        if (block is IMyTextSurface) {
          if (!block.CustomName.Contains (defaultKeyword))
            addDefaultKW = true;
          if (!block.CustomData.Contains (keyword))
            block.CustomData = "@0 " + keyword + (defaultOptions != null ? "\n" + String.Join ("\n", defaultOptions) : "");
        } else if (block is IMyTextSurfaceProvider) {
          if (!block.CustomName.Contains (defaultKeyword))
            addDefaultKW = true;
          int Ŵ = (block as IMyTextSurfaceProvider).SurfaceCount;
          for (int O = 0; O < Ŵ; O++) {
            if (!block.CustomData.Contains ("@" + O)) {
              block.CustomData += (block.CustomData == "" ? "" : "\n\n") + "@" + O + " " + keyword + (defaultOptions != null ? "\n" + String.Join ("\n", defaultOptions) : "");
              break;
            }
          }
        } else {
          blocks.Remove (block);
        }
        if (addDefaultKW)
          block.CustomName += " " + defaultKeyword;
      }
      return blocks;
    }
  }
  public static partial class Helper {
    public static float Ratio (this float operand, float dividend) {
      return dividend == 0 ? 1 : operand / dividend;
    }
    public static double Ratio (this double operand, double dividend) {
      return dividend == 0 ? 1 : operand / dividend;
    }
  }
  public static partial class Helper {
    private static Dictionary<char, float> Widths = new Dictionary<char, float> ();
    public static void InitWidth (string chars, float width) {
      foreach (char c in chars) {
        Widths[c] = width;
      }
    }
    public static void InitWidths () {
      if (Widths.Count > 0)
        return;
      InitWidth ("3FKTabdeghknopqsuy£µÝàáâãäåèéêëðñòóôõöøùúûüýþÿāăąďđēĕėęěĝğġģĥħĶķńņňŉōŏőśŝşšŢŤŦũūŭůűųŶŷŸșȚЎЗКЛбдекруцяёђћўџ", 18);
      InitWidth ("ABDNOQRSÀÁÂÃÄÅÐÑÒÓÔÕÖØĂĄĎĐŃŅŇŌŎŐŔŖŘŚŜŞŠȘЅЊЖф□", 22);
      InitWidth ("#0245689CXZ¤¥ÇßĆĈĊČŹŻŽƒЁЌАБВДИЙПРСТУХЬ€", 20);
      InitWidth ("￥$&GHPUVY§ÙÚÛÜÞĀĜĞĠĢĤĦŨŪŬŮŰŲОФЦЪЯжы†‡", 21);
      InitWidth ("！ !I`ijl ¡¨¯´¸ÌÍÎÏìíîïĨĩĪīĮįİıĵĺļľłˆˇ˘˙˚˛˜˝ІЇії‹›∙", 9);
      InitWidth ("？7?Jcz¢¿çćĉċčĴźżžЃЈЧавийнопсъьѓѕќ", 17);
      InitWidth ("（）：《》，。、；【】(),.1:;[]ft{}·ţťŧț", 10);
      InitWidth ("+<=>E^~¬±¶ÈÉÊË×÷ĒĔĖĘĚЄЏЕНЭ−", 19);
      InitWidth ("L_vx«»ĹĻĽĿŁГгзлхчҐ–•", 16);
      InitWidth ("\"-rª­ºŀŕŗř", 11);
      InitWidth ("WÆŒŴ—…‰", 32);
      InitWidth ("'|¦ˉ‘’‚", 7);
      InitWidth ("@©®мшњ", 26);
      InitWidth ("mw¼ŵЮщ", 28);
      InitWidth ("/ĳтэє", 15);
      InitWidth ("\\°“”„", 13);
      InitWidth ("*²³¹", 12);
      InitWidth ("¾æœЉ", 29);
      InitWidth ("%ĲЫ", 25);
      InitWidth ("MМШ", 27);
      InitWidth ("½Щ", 30);
      InitWidth ("ю", 24);
      InitWidth ("ј", 8);
      InitWidth ("љ", 23);
      InitWidth ("ґ", 14);
      InitWidth ("™", 31);
    }
    public static Vector2 Ż(this IMyTextSurface æ, StringBuilder z) {
      InitWidths ();
      Vector2 È = new Vector2 ();
      if (æ.Font == "Monospace") { float å = æ.FontSize; È.X = (float) (z.Length * 19.4 * å); È.Y = (float) (28.8 * å); return È; } else {
        float å = (float) (æ.FontSize * 0.779);
        foreach (char ū in z.ToString ()) { try { È.X += Widths[ū] * å; } catch { } }
        È.Y = (float) (28.8 * æ.FontSize);
        return È;
      }
    }
    public static float Ŭ(this IMyTextSurface Æ, StringBuilder z) { Vector2 ŭ = Æ.Ż(z); return ŭ.X; }
    public static float Ŭ(this IMyTextSurface Æ, string z) {
      Vector2 ŭ = Æ.Ż(new StringBuilder (z));
      return ŭ.X;
    }
    public static float Ų(this IMyTextSurface Æ, char Ů) {
      float ů = Ŭ(Æ, new string (Ů, 1));
      return ů;
    }
    public static int Ű(this IMyTextSurface Æ) {
      Vector2 Ğ = Æ.SurfaceSize;
      float ġ = Æ.TextureSize.Y;
      Ğ.Y *= 512 / ġ;
      float ű = Ğ.Y * (100 - Æ.TextPadding * 2) / 100;
      Vector2 ŭ = Æ.Ż(new StringBuilder ("T"));
      return (int) (ű / ŭ.Y);
    }
    public static float ř(this IMyTextSurface Æ) {
      Vector2 Ğ = Æ.SurfaceSize;
      float ġ = Æ.TextureSize.Y;
      Ğ.X *= 512 / ġ;
      return Ğ.X * (100 - Æ.TextPadding * 2) / 100;
    }
    public static StringBuilder Ģ(this IMyTextSurface Æ, char ģ, double Ĥ) { int ĥ = (int) (Ĥ / Ų(Æ, ģ)); if (ĥ < 0) ĥ = 0; return new StringBuilder ().Append (ģ, ĥ); }
    private static DateTime Ħ =
      DateTime.Now;
    private static Dictionary<int, List<int>> ħ = new Dictionary<int, List<int>> ();
    public static StringBuilder Ĩ(this IMyTextSurface Æ, StringBuilder z, int ĩ = 3, bool X = true, int Ī = 0) {
      int ī = Æ.GetHashCode ();
      if (!ħ.ContainsKey (ī)) {
        ħ[ī] = new List<int> { 1, 3, ĩ, 0 };
      }
      int Ĭ = ħ[ī][0];
      int ĭ = ħ[ī][1];
      int Į = ħ[ī][2];
      int į = ħ[ī][3];
      var ı = z.ToString ().TrimEnd ('\n').Split ('\n');
      List<string> ğ = new
      List<string> ();
      if (Ī == 0)
        Ī = Æ.Ű();
      float Ò = Æ.ř();
      StringBuilder Ė, ď = new StringBuilder ();
      for (int O = 0; O < ı.Length; O++) {
        if (O < ĩ || O < Į ||
          ğ.Count - Į > Ī || Æ.Ŭ(ı[O]) <= Ò) { ğ.Add (ı[O]); } else {
          try {
            ď.Clear ();
            float Đ, đ;
            var Ē = ı[O].Split (' ');
            string ē = System.Text.
            RegularExpressions.Regex.Match (ı[O], @"\d+(\.|\:)\ ").Value;
            Ė = Æ.Ģ(' ', Æ.Ŭ(ē));
            foreach (var Ĕ in Ē) {
              Đ = Æ.Ŭ(ď);
              đ = Æ.Ŭ(Ĕ);
              if (Đ + đ > Ò) {
                ğ.Add (ď.ToString ());
                ď = new StringBuilder (Ė + Ĕ + " ");
              } else { ď.Append (Ĕ + " "); }
            }
            ğ.Add (ď.ToString ());
          } catch { ğ.Add (ı[O]); }
        }
      }
      if (X) {
        if (ğ.Count > Ī) {
          if (DateTime.Now.Second != į) {
            į = DateTime.Now.Second;
            if (ĭ > 0)
              ĭ--;
            if (ĭ <= 0)
              Į += Ĭ;
            if (Į + Ī - ĩ >= ğ.Count && ĭ <= 0) { Ĭ = -1; ĭ = 3; }
            if (Į <= ĩ && ĭ <= 0) { Ĭ = 1; ĭ = 3; }
          }
        } else { Į = ĩ; Ĭ = 1; ĭ = 3; }
        ħ[ī][0] = Ĭ;
        ħ[ī][1] = ĭ;
        ħ[ī][2] = Į;
        ħ[ī][3] = į;
      } else { Į = ĩ; }
      StringBuilder ĕ = new StringBuilder ();
      for (
        var Ø = 0; Ø < ĩ; Ø++) { ĕ.Append (ğ[Ø] + "\n"); }
      for (var Ø = Į; Ø < ğ.Count; Ø++) { ĕ.Append (ğ[Ø] + "\n"); }
      return ĕ;
    }
    public static Dictionary<IMyTextSurface, string> ė(this IMyTerminalBlock ú, string Ę, Dictionary<string, string> ę = null) {
      var Ě = new Dictionary<IMyTextSurface, string> ();
      if (ú is IMyTextSurface) {
        Ě[ú as IMyTextSurface] = ú.CustomData;
      } else if (ú is IMyTextSurfaceProvider) {
        var ě = System.Text.RegularExpressions.Regex.Matches (ú.CustomData, @"@(\d) *(" + Ę + @")");
        int Ĝ = (ú as IMyTextSurfaceProvider).SurfaceCount;
        foreach (System.Text.RegularExpressions.Match ĝ in ě) {
          int İ = -1;
          if (int.TryParse (ĝ.Groups[1].Value, out İ)) {
            if (İ >= Ĝ)
              continue;
            string Ĳ = ú.CustomData;
            int ŉ = Ĳ.IndexOf ("@" + İ);
            int ŀ = Ĳ.IndexOf ("@", ŉ + 1) - ŉ;
            string T = ŀ <= 0 ? Ĳ.Substring (ŉ) : Ĳ.Substring (ŉ, ŀ);
            Ě[(ú as IMyTextSurfaceProvider).GetSurface (İ)] = T;
          }
        }
      }
      return Ě;
    }
    public static bool GetBool (this string text, string key) {
      var lines = text.Replace (" ", "").Split ('\n');
      foreach (var line in lines) {
        if (line.StartsWith (key + "=")) {
          try {
            return Convert.ToBoolean (line.Replace (key + "=", ""));
          } catch {
            return true;
          }
        }
      }
      return true;
    }
    public static string GetValue (this string text, string key) {
      var lines = text.Replace (" ", "").Split ('\n');
      foreach (var line in lines) {
        if (line.StartsWith (key + "=")) {
          return line.Replace (key + "=", "");
        }
      }
      return "";
    }
  }
  public static partial class Helper {
    public static bool IsBetween (this double num, double min, double max, bool strictMax = false, bool strictMin = false) {
      bool isGreater = num >= min;
      bool isSmaller = num <= max;
      if (strictMin) isGreater = num > min;
      if (strictMax) isSmaller = num < max;
      return isGreater && isSmaller;
    }
  }
  public static partial class Helper {
    public static string Repeat (this char c, int n) {
      if (n <= 0) {
        return "";
      }
      return new string (c, n);
    }
  }
  public static partial class Helper {
    public static string FormatPercent (this double operand, double dividend) {
      double Ĺ = Math.Round (operand / dividend * 100, 1);
      if (dividend == 0) {
        return "0%";
      } else {
        return Ĺ + "%";
      }
    }
    public static string FormatPercent (this float operand, float dividend) {
      double Ĺ = Math.Round (operand / dividend * 100, 1);
      if (dividend == 0) {
        return "0%";
      } else {
        return Ĺ + "%";
      }
    }
  }
  public static partial class Helper {
    public static string Format (this float num, bool isEnergy = false) {
      string unit = "MW";
      string sign = num < 0 ? "-" : "";
      num = Math.Abs (num);
      if (num < 1) {
        num *= 1000;
        unit = "kW";
      } else if (num >= 1000 && num < 1000000) {
        num /= 1000;
        unit = "GW";
      } else if (num >= 1000000 && num < 1000000000) {
        num /= 1000000;
        unit = "TW";
      } else if (num >= 1000000000) {
        num /= 1000000000;
        unit = "PW";
      }
      if (isEnergy)
        unit += "h";
      return sign + Math.Round (num, 1) + " " + unit;
    }
  }
  public static partial class Helper {
    public static string Format (this double num) {
      string unit = "L";
      if (num >= 1000 && num < 1000000) {
        num /= 1000;
        unit = "KL";
      } else if (num >= 1000000 && num < 1000000000) {
        num /= 1000000;
        unit = "ML";
      } else if (num >= 1000000000 && num < 1000000000000) {
        num /= 1000000000;
        unit = "BL";
      } else if (num >= 1000000000000) {
        num /= 1000000000000;
        unit = "TL";
      }
      return Math.Round (num, 1) + " " + unit;
    }
  }
}