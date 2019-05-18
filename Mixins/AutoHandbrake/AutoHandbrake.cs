using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public interface IAHDeactivator {
      bool ShouldDeactivate();
    }
    public interface IAHBraker {
      bool ShouldHandbrake();
    }
    public class AutoHandbrake {
      const string SECTION = "auto-handbrake";
      readonly List<IMyShipController> _controllers = new List<IMyShipController>(3);
      readonly List<IAHDeactivator> _deactivators = new List<IAHDeactivator>();
      readonly List<IAHBraker> _hbrakers = new List<IAHBraker>();
      bool _wasPrvslyAutoBraked;
      bool _manualBrake;

      public AutoHandbrake(MyIni ini, IMyGridTerminalSystem gts) {
        _manualBrake = ini.Get(SECTION, "manual").ToBoolean();
        string[] names = ini.Get(SECTION, "controllers").ToString().Split(new char[] { ',' });
        foreach(string name in names) {
          var cont = gts.GetBlockWithName(name) as IMyShipController;
          if (cont != null) {
            _controllers.Add(cont);
          } else {
            Logger.Inst.Log($"Could not find ship controller {name}");
          }
        }
        if (_controllers.Count == 0) {
          throw new InvalidOperationException($"No controller found");
        } else {
          Logger.Inst.Log($"Auto handbrake... {_controllers.Count}/{names.Count()} OK");
        }
        if (!_shouldBrake() && _isBraked()) {
          _manualBrake = true;
        }
        _wasPrvslyAutoBraked = _shouldBrake();
        Scheduler.Inst.AddAction(new ScheduledAction(_handleHandbrake, period: 10));
        Scheduler.Inst.AddActionOnSave(_save);
      }
      public void AddDeactivator(IAHDeactivator d) => _deactivators.Add(d);
      public void AddBraker(IAHBraker h) => _hbrakers.Add(h);
      bool _isDeactivated() => _deactivators.Any(d => d.ShouldDeactivate());
      void _handleHandbrake() {
        if (!_wasPrvslyAutoBraked && _isBraked()) {
          _manualBrake = true;
        } else if (_manualBrake && !_isBraked()) {
          _manualBrake = false;
        }
        _wasPrvslyAutoBraked = _shouldBrake();
        _controllers.First().HandBrake = _wasPrvslyAutoBraked || _manualBrake;
      }
      bool _isBraked() => _controllers.First().HandBrake;
      void _save(MyIni ini) {
        ini.Set(SECTION, "manual", _manualBrake);
        ini.Set(SECTION, "controllers", string.Join(",", _controllers.Select(c => c.DisplayNameText)));
      }
      bool _shouldBrake() => _controllers.All(c => !c.IsUnderControl) || _hbrakers.Any(h => h.ShouldHandbrake());
    }
  }
}
