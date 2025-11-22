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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Interface to implement to be able to automatically disable the pilot assistance</summary>
    public interface IPADeactivator { bool ShouldDeactivate(); }
    /// <summary>Interface to implement to be able to automatically engage brakes</summary>
    public interface IPABraker { bool ShouldHandbrake(); }
    /// <summary>
    /// <para>Class that automatically engages the hand brake when there is no pilot and allows finer wheel control with a pad.</para>
    /// <para>It maintains a list of controllers, and if there is no pilot in any of the controllers, it engages the handbrake.</para>
    /// </summary>
    public class PilotAssist : IIniConsumer
    {
      const string SECTION = "pilot-assist";

      public bool ManuallyBraked { get; private set; }
      bool Braked => _controllers.First().HandBrake;
      bool Deactivated => _deactivators.Any(d => d.ShouldDeactivate());

      readonly List<IMyShipController> _controllers = new List<IMyShipController>(3);
      readonly List<IPADeactivator> _deactivators = new List<IPADeactivator>();
      readonly List<IPABraker> _handBrakers = new List<IPABraker>();
      readonly IMyGridTerminalSystem _gts;
      readonly Action<string> _logger;
      readonly IMyTerminalBlock _refBlock;
      readonly WheelsController _wheelControllers;
      bool _assist;
      float _sensitivity;
      bool _wasPreviouslyAutoBraked;
      float? _cruiseSpeed = null;
      /// <summary>Creates a PilotAssist</summary>
      /// <param name="gts">To get the different blocks</param>
      /// <param name="ini">Parsed ini that contains the configuration. See <see cref="Read(Ini)"/> for more information</param>
      /// <param name="logger">Optional logger</param>
      /// <param name="manager">Used to schedule itself</param>
      /// <param name="wc">Wheel controller used to actually controll the wheels</param>
      public PilotAssist(IMyTerminalBlock refBlock, IMyGridTerminalSystem gts, IniWatcher ini, Action<string> logger, ISaveManager manager, WheelsController wc, CommandLine commandLine = null)
      {
        _logger = logger;
        _wheelControllers = wc;
        _gts = gts;
        _refBlock = refBlock;
        Read(ini);
        ManuallyBraked = !_shouldBrake() && Braked;
        _wasPreviouslyAutoBraked = _shouldBrake();
        manager.Spawn(_handle, "pilot-assist");
        manager.AddOnSave(_save);
        ini.Add(this);
        commandLine?.RegisterCommand(new Command("pa-cruise", Command.Wrap(_setCruiseControl), "Sets a cruise control", nArgs: 1));
      }
      /// <summary>Adds an object that is polled to see if the automatic handbrake should be engaged, on top of the default behaviour.</summary>
      /// <param name="d">Deactivator. If any registered braker returns true, the automatic brakes are engaged, unless deactivated.</param>
      public void AddBraker(IPABraker h) => _handBrakers.Add(h);
      /// <summary>Adds an object that is polled to see if the automatic handbrake should be deactivated.</summary>
      /// <param name="d">Deactivator. If any registered deactivator returns true, the automatic brakes are disengaged.</param>
      public void AddDeactivator(IPADeactivator d) => _deactivators.Add(d);
      /// <summary>
      /// Can be called to update the configuration without restarting the program. Called at creation time.
      /// <para>The ini must contain a section named <see cref="SECTION"/> which contains:</para>
      /// <list type="bullet">
      /// <item><b>assist</b>: whether the fine controls are active: it overrides the normal controls.</item>
      /// <item><b>controllers</b>: names of the controllers considered by pilot assist. At least one valid controller is needed.</item>
      /// <item><b>sensitivity</b>: maximum value the controller input can take. The higher the less sensitive</item>
      /// </list>
      /// </summary>
      /// <param name="ini">The configuration</param>
      public void Read(MyIni ini)
      {
        _assist = ini.Get(SECTION, "assist").ToBoolean();
        _controllers.Clear();
        _sensitivity = ini.Get(SECTION, "sensitivity").ToSingle(2f);
        var controllerNames = ini.Get(SECTION, "controllers");
        string[] names = controllerNames.ToString().Split(IniHelper.SEP, StringSplitOptions.RemoveEmptyEntries);
        _gts.GetBlocksOfType(_controllers, c => c.IsSameConstructAs(_refBlock) && (names.Length == 0 || names.Contains(c.CustomName)));
        
        if (_controllers.Count == 0)
        {
          throw new InvalidOperationException("No controller found");
        }
      }
      void _setCruiseControl(string arg)
      {
        if (arg == "off")
        {
          _cruiseSpeed = null;
        }
        else
        {
          _cruiseSpeed = float.Parse(arg);
        }
      }

      void _handle(Process p)
      {
        if (Deactivated)
        {
          _cruiseSpeed = null;
          // Deactivate cruise control if auto pilot is engaged
          _controllers.Select(c => c.MoveIndicator.Z).Sum();
        }
        else
        {
          // Keep track of whether the hand brake was manually engage not to de engage it automatically when a pilot re enters a cockpit
          if (!_wasPreviouslyAutoBraked && Braked)
          {
            ManuallyBraked = true;
          }
          else if (ManuallyBraked && !Braked)
          {
            ManuallyBraked = false;
          }
          _wasPreviouslyAutoBraked = _shouldBrake();
          _controllers.First().HandBrake = _wasPreviouslyAutoBraked || ManuallyBraked;

          // Deactivate cruise control if pilot inputs forward or backward direction or the brake
          var pilotForwardInput = _controllers.Select(c => c.MoveIndicator.Z).Sum();
          if (_cruiseSpeed != null && (ManuallyBraked || pilotForwardInput != 0))
          {
            _cruiseSpeed = null;
            _wheelControllers.SetPower(0);
          }

          if (_cruiseSpeed != null)
          {
            // TODO PID ?
            var currentSpeed = (float)_controllers.First().GetShipSpeed();
            _wheelControllers.SetPower((_cruiseSpeed.Value - currentSpeed) / _sensitivity);
          }
          if (_assist)
          {
            _wheelControllers.SetPower(-pilotForwardInput / _sensitivity);
            float right = _controllers.Sum(c => c.MoveIndicator.X);
            _wheelControllers.SetSteer(right == 1 ? 1 : right / _sensitivity);
          }
        }
      }
      void _save(MyIni ini)
      {
        ini.Set(SECTION, "assist", _assist);
        ini.Set(SECTION, "controllers", string.Join(",", _controllers.Select(c => c.CustomName)));
        ini.Set(SECTION, "sensitivity", _sensitivity);
      }
      bool _shouldBrake()
      {
        // To engage the handbrake when the pilot presses the up key (esp. useful when controlling with the pad)
        float up = _assist ? _controllers.Sum(c => c.MoveIndicator.Y) : 0;
        return _controllers.All(c => !c.IsUnderControl) || _handBrakers.Any(h => h.ShouldHandbrake()) || up == 1;
      }
    }
  }
}
