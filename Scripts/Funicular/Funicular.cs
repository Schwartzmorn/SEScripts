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
  public partial class Program : MyGridProgram
  {
    enum FunicularCommand
    {
      Stop,
      MoveUp,
      MoveDown,
    }

    enum FunicularState
    {
      Unlocking,
      Moving,
      Deccelerating,
      Locking,
      Locked,
    }

    public class FunicularSettings
    {
      public float DeccelerationDistance;
      public float MaxAcceleration;
      public float MaxSpeed;
      public float SafeSpeed;
    }

    public class Funicular : IIniConsumer
    {
      readonly static string SECTION = "Funicular";

      Vector3D _bottomPosition;
      Vector3D _topPosition;
      FunicularCommand _currentCommand = FunicularCommand.Stop;
      FunicularState _state;
      readonly List<IMyPistonBase> _pistons = new List<IMyPistonBase>();
      readonly List<IMyLandingGear> _plates = new List<IMyLandingGear>();
      readonly List<Rotor> _rotors;
      readonly List<FunicularDisplay> _displays;
      readonly IMyTerminalBlock _referenceBlock;
      readonly IMyShipConnector _connector;
      readonly FunicularSettings _settings = new FunicularSettings();

      float _distance;

      public Funicular(IMyGridTerminalSystem gts, IMyTerminalBlock refBlock, IProcessManager manager, CommandLine cmd, IniWatcher ini)
      {
        _referenceBlock = refBlock;
        Read(ini);
        ini.Add(this);
        var process = manager.Spawn(Handle, "funicular-main");
        gts.GetBlocksOfType(_pistons, p => p.CubeGrid == _referenceBlock.CubeGrid);
        gts.GetBlocksOfType(_plates, l => _pistons.Any(p => p.TopGrid == l.CubeGrid));
        _plates.ForEach(p => p.AutoLock = false);
        var stators = new List<IMyMotorStator>();
        gts.GetBlocksOfType(stators, s => s.CubeGrid == _referenceBlock.CubeGrid);
        _rotors = stators.Select(s => new Rotor(s, s.WorldMatrix.Up.Dot(_referenceBlock.WorldMatrix.Left) > 0, _settings)).ToList();
        var connectors = new List<IMyShipConnector>();
        gts.GetBlocksOfType(connectors, c => c.CubeGrid == _referenceBlock.CubeGrid);
        _connector = connectors.FirstOrDefault();
        var parentCommand = new ParentCommand("funicular", "Interacts with the funicular")
          .AddSubCommand(new Command("stop", Command.Wrap(Stop), "Stops the funicular", nArgs: 0))
          .AddSubCommand(new Command("move", Command.Wrap(_wrap(Move)), "Commands the funicular to move.\nArgument is \"up\" or \"down\".", nArgs: 1))
          .AddSubCommand(new Command("save", Command.Wrap(_wrap(Save)), "Save the position.\nArgument is \"up\" or \"down\".", nArgs: 1))
          .AddSubCommand(new Command("forget", Command.Wrap(_wrap(Forget)), "Forgets the position.\nArgument is \"up\" or \"down\".", nArgs: 1));
        cmd.RegisterCommand(parentCommand);
        var textPanels = new List<IMyTextPanel>();
        gts.GetBlocksOfType(textPanels, p => p.CubeGrid == _referenceBlock.CubeGrid);
        _displays = textPanels.Select(t => new FunicularDisplay(_referenceBlock, t)).ToList();
        _state = _currentCommand == FunicularCommand.Stop ? FunicularState.Locking : FunicularState.Unlocking;
        Handle(process);
        process.Spawn(p => UpdateDisplays(), "display-update", period: 10);
      }

      public void Handle(Process process)
      {
        switch (_currentCommand)
        {
          case FunicularCommand.Stop: { Stop(); break; }
          case FunicularCommand.MoveUp: { Move(true); break; }
          case FunicularCommand.MoveDown: { Move(false); break; }
        }
      }

      public void Stop()
      {
        _currentCommand = FunicularCommand.Stop;
        bool stopped = true;
        foreach (var r in _rotors)
        {
          stopped &= r.Stop();
        }
        bool extending = false;
        foreach (var p in _pistons)
        {
          if (1 - p.NormalizedPosition > 0.01)
          {
            extending = true;
            p.Enabled = true;
            p.Extend();
          }
        }
        if (extending)
        {
          _state = FunicularState.Locking;
          return;
        }
        _state = FunicularState.Locked;
        foreach (var p in _plates)
        {
          p.Enabled = true;
          p.Lock();
        }
        _connector.Enabled = true;
        if (_connector?.Status == MyShipConnectorStatus.Connectable)
        {
          _connector.Connect();
        }
      }

      Action<string> _wrap(Action<bool> action)
      {
        return arg =>
        {
          if (arg == "up")
          {
            action(true);
          }
          else if (arg == "down")
          {
            action(false);
          }
          else
          {
            throw new ArgumentException("Argument must be \"up\" or \"down\"");
          }
        };
      }

      public void Move(bool up)
      {
        var target = up ? _topPosition : _bottomPosition;

        float distance = target == Vector3D.Zero ? -1f : (float)(target - _referenceBlock.GetPosition()).Length();
        _distance = distance;

        if (Math.Abs(distance) < 0.05)
        {
          Stop();
          return;
        }

        _currentCommand = up ? FunicularCommand.MoveUp : FunicularCommand.MoveDown;
        foreach (var p in _plates)
        {
          p.Unlock();
          p.Enabled = false;
        }
        var retracting = false;
        foreach (var p in _pistons)
        {
          if (p.NormalizedPosition > 0.01)
          {
            p.Enabled = true;
            p.Retract();
            retracting = true;
          }
        }
        if (retracting)
        {
          _state = FunicularState.Unlocking;
          return;
        }
        foreach (var p in _pistons)
        {
          p.Enabled = false;
        }
        _connector.Disconnect();
        _connector.Enabled = false;

        if (Math.Abs(distance) < _settings.DeccelerationDistance)
        {
          _state = FunicularState.Deccelerating;
        }
        else
        {
          _state = FunicularState.Moving;
        }

        foreach (var r in _rotors)
        {
          r.Move(up, distance);
        }
      }

      public void Save(bool up)
      {
        var position = _referenceBlock.GetPosition();
        if (up)
        {
          _topPosition = position;
        }
        else
        {
          _bottomPosition = position;
        }
      }

      public void Forget(bool up)
      {
        if (up)
        {
          _topPosition = Vector3D.Zero;
        }
        else
        {
          _bottomPosition = Vector3D.Zero;
        }
      }

      public void UpdateDisplays()
      {
        _displays.ForEach(d => d.UpdateStatus(_currentCommand, _state, _distance, _bottomPosition, _topPosition));
      }

      public void OnSave(MyIni myIni)
      {
        myIni.Set(SECTION, "state", _currentCommand.ToString());
        myIni.Set(SECTION, "decelerationDistance", _settings.DeccelerationDistance);
        myIni.Set(SECTION, "maxAcceleration", _settings.MaxAcceleration);
        myIni.Set(SECTION, "maxSpeed", _settings.MaxSpeed);
        myIni.Set(SECTION, "safeSpeed", _settings.SafeSpeed);
        if (_bottomPosition != Vector3D.Zero)
        {
          myIni.SetVector(SECTION, "bottomPosition", _bottomPosition);
        }
        if (_topPosition != Vector3D.Zero)
        {
          myIni.SetVector(SECTION, "topPosition", _topPosition);
        }
      }

      public void Read(MyIni ini)
      {
        var stringState = ini.Get(SECTION, "state").ToString();
        Enum.TryParse(stringState, out _currentCommand);

        _settings.DeccelerationDistance = (float)ini.Get(SECTION, "decelerationDistance").ToDouble(5);
        _settings.MaxAcceleration = (float)ini.Get(SECTION, "maxAcceleration").ToDouble(0.1);
        _settings.MaxSpeed = (float)ini.Get(SECTION, "maxSpeed").ToDouble(20);
        _settings.SafeSpeed = (float)ini.Get(SECTION, "safeSpeed").ToDouble(5);

        if (ini.ContainsKey(SECTION, "bottomPosition-x"))
        {
          _bottomPosition = ini.GetVector(SECTION, "bottomPosition");
        }

        if (ini.ContainsKey(SECTION, "topPosition-x"))
        {
          _topPosition = ini.GetVector(SECTION, "topPosition");
        }
      }
    }
  }
}
