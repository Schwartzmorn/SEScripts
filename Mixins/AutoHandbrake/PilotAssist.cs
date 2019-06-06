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
  public interface IPADeactivator { bool ShouldDeactivate(); }
  public interface IPABraker {  bool ShouldHandbrake(); }
  public class PilotAssist: IIniConsumer {
    const string SECTION = "pilot-assist";
    readonly List<IMyShipController> _cont = new List<IMyShipController>(3);
    readonly List<IPADeactivator> _deact = new List<IPADeactivator>();
    readonly List<IPABraker> _hbrakers = new List<IPABraker>();
    readonly IMyGridTerminalSystem _gts;
    readonly WheelsController _wc;
    bool _assist;
    bool _manualBrake;
    bool _wasPrvslyAutoBraked;

    public PilotAssist(Ini ini, WheelsController wc, IMyGridTerminalSystem gts) {
      _wc = wc;
      _gts = gts;
      Read(ini);
      _manualBrake = ini.Get(SECTION, "manual").ToBoolean();
      if(!_shouldBrake() && _braked)
        _manualBrake = true;
      _wasPrvslyAutoBraked = _shouldBrake();
      Schedule(new ScheduledAction(_handle, name: "pa-handle"));
      ScheduleOnSave(_save);
    }
    public void Read(Ini ini) {
      _assist = ini.Get(SECTION, "assist").ToBoolean();
      _cont.Clear();
      string[] names = ini.GetThrow(SECTION, "controllers").ToString().Split(new char[] { ',' });
      foreach(string s in names) {
        var cont = _gts.GetBlockWithName(s) as IMyShipController;
        if(cont != null) {
          cont.ControlWheels = !_assist;
          _cont.Add(cont);
        } else
          Log($"Could not find ship controller {s}");
      }
      if(_cont.Count == 0)
        throw new InvalidOperationException($"No controller found");
    }
    public void AddDeactivator(IPADeactivator d) => _deact.Add(d);
    public void AddBraker(IPABraker h) => _hbrakers.Add(h);
    bool _deactivated => _deact.Any(d => d.ShouldDeactivate());
    void _handle() {
      if(!_deactivated) {
        if(!_wasPrvslyAutoBraked && _braked)
          _manualBrake = true;
        else if(_manualBrake && !_braked)
          _manualBrake = false;
        _wasPrvslyAutoBraked = _shouldBrake();
        _cont.First().HandBrake = _wasPrvslyAutoBraked || _manualBrake;
        if (_assist) {
          _wc.SetPower(-_cont.Sum(c => c.MoveIndicator.Z) / 2);
          float right = _cont.Sum(c => c.MoveIndicator.X);
          _wc.SetSteer(right == 1 ? 1 : right / 2);
        }
      }
    }
    bool _braked => _cont.First().HandBrake;
    void _save(MyIni ini) {
      ini.Set(SECTION, "manual", _manualBrake);
      ini.Set(SECTION, "controllers", string.Join(",", _cont.Select(c => c.DisplayNameText)));
      ini.Set(SECTION, "assist", _assist);
    }
    bool _shouldBrake() {
      float up = _assist ? _cont.Sum(c => c.MoveIndicator.Y) : 0;
      return _cont.All(c => !c.IsUnderControl) || _hbrakers.Any(h => h.ShouldHandbrake()) || up == 1;
    }
  }
}
}
