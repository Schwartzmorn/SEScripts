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
  partial class Program {
    public class SolarPylon {
      public SolarPylon(String pylonKey) {
        _pylonKey = pylonKey;
        _vRotor = new SolarRotor(_pylonKey, SolarRotorType.Vertical);
        _hRotors.Add(new SolarRotor(_pylonKey, SolarRotorType.HorizontalLeft));
        _hRotors.Add(new SolarRotor(_pylonKey, SolarRotorType.HorizontalRight));
        _panel = new SolarPanel(_hRotors);
      }
      public float MaxOutput {
        get {
          return _panel.MaxOutput;
        }
      }
      public float VerticalPosition {
        get {
          return _vRotor.GetPosition();
        }
      }
      public float HorizontalPosition {
        get {
          foreach (SolarRotor rotor in _hRotors) {
            if (rotor.GetRotor() != null) {
              return rotor.GetPosition();
            }
          }
          return 0;
        }
      }
      public void Stop() {
        _vRotor.Move(0);
        foreach (SolarRotor rotor in _hRotors) {
          rotor.Move(0);
        }
      }
      public bool HasReachedEndHorizontally(bool forNight) {
        foreach (SolarRotor rotor in _hRotors) {
          if (!rotor.HasReachedEnd(forNight)) {
            return false;
          }
        }
        return true;
      }
      public void RotateVertically(float speed) => _vRotor.Move(speed);
      public void RotateHorizontally(float speed) {
        foreach (SolarRotor rotor in _hRotors) {
          rotor.Move(speed);
        }
      }
      public String GetName() => "Solar pylon " + _pylonKey;
      public void UpdateFromGrid() => _panel.UpdateFromGrid();
      public string GetKey() => _pylonKey;
      private SolarPanel _panel;
      private List<SolarRotor> _hRotors = new List<SolarRotor>();
      private SolarRotor _vRotor;
      private String _pylonKey;
    }
  }
}
