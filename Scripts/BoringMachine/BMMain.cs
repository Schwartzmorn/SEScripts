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

namespace IngameScript {
  partial class Program : MyGridProgram {
    readonly CmdLine _command;
    ColorScheme _scheme = new ColorScheme();

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      IMyTextSurface topLeft, topRight, keyboard;
      IMyCockpit cockpit;
      _initCockpit(out cockpit, out topLeft, out topRight, out keyboard);
      var ct = new CoordsTransformer(cockpit, true);
      // Can't use logger before that
      _initLogger(keyboard);
      _command = new CmdLine("Boring machine", _log);
      var genStatus = new GeneralStatus(this);
      var ini = new MyIni();
      MyIniParseResult res;
      if(!ini.TryParse(Me.CustomData, out res)) {
        _log($"Error at line of custom data {res.LineNo}");
      }
      var wc = new WheelsController(this, ini, _command, ct, cockpit);
      var ac = new ArmController(this, ini, _command, cockpit, wc);
      var ic = new InventoriesController(ct, GridTerminalSystem, cockpit, wc.GetIdealCenterOfMass() + 2);
      var client = new ConnectionClient(this, ini, _command, "StationConnectionRequests", "BMConnections");
      new ScreensController(ac, genStatus, ic, client, topLeft, topRight, _scheme, cockpit.CustomData);
      var ap = new Autopilot(wc, _command, GridTerminalSystem.GetBlockWithName("BM Remote Control (Forward)") as IMyRemoteControl);
      var ah = new AutoHandbrake(ini, GridTerminalSystem);
      ah.AddBraker(client);
      ah.AddDeactivator(ap);
      _log("Boot sequence done");
    }

    public void Save() => Scheduler.Inst.Save(s => Me.CustomData = s);

    public void Main(string argument, UpdateType updateSource) {
      _command.HandleCmd(argument, true);
      Scheduler.Inst.Tick();
    }

    void _initCockpit(out IMyCockpit cockpit, out IMyTextSurface topLeft, out IMyTextSurface topRight, out IMyTextSurface keyboard) {
      var cockpits = new List<IMyCockpit>();
      GridTerminalSystem.GetBlocksOfType(cockpits, c => c.CubeGrid == Me.CubeGrid);
      if (cockpits.Count == 0) {
        throw new ArgumentException("No cockpit found");
      }
      cockpit = cockpits[0];
      topLeft = null;
      topRight = null;
      keyboard = null;
      for(int i = 0; i < cockpit.SurfaceCount; ++i) {
        var surface = cockpit.GetSurface(i);
        if(surface.DisplayName == "Top Left Screen") {
          topLeft = surface;
        } else if(surface.DisplayName == "Top Right Screen") {
          topRight = surface;
        } else if(surface.DisplayName == "Keyboard") {
          keyboard = surface;
        }
      }
    }

    void _log(string s) => Logger.Inst.Log(s);

    void _initLogger(IMyTextSurface keyboard) {
      Logger.SetupGlobalInstance(new Logger(keyboard, fontSize: 1.0f, fontColor: _scheme.Light, bgdColor: _scheme.Dark), Echo);
      Scheduler.Inst.AddAction(Logger.Flush);
      _log("Booting up...");
    }
  }
}