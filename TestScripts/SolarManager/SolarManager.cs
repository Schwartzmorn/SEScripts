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
    public class SolarManager {
      public SolarManager(string witnessKey) {
        AddPylon(witnessKey);
        _witness = _pylons[witnessKey];
      }
      public bool HasOutput(float ratio = 0.01f) => MaxOutput != 0 &&
            MaxOutput > _maxHistoricalOutput * ratio;
      public bool HasOutputDiminished() {
        int numAverage = 2;
        float lastTwoValues = _lastOutputs.AverageLastN(numAverage, _historyIndex);
        return lastTwoValues < _lastOutputs[_historyIndex - numAverage];
      }
      public void StopChasing() {
        foreach (SolarPylon pylon in _pylons.Values) {
          pylon.Stop();
        }
      }
      public bool HasReachedEnd(bool forNight) {
        foreach (SolarPylon pylon in _pylons.Values) {
          if (!pylon.HasReachedEndHorizontally(forNight)) {
            return false;
          }
          if (!forNight && Math.Abs(pylon.VerticalPosition) > ALIGNMENT_CUTOFF) {
            return false;
          }
        }
        return true;
      }
      public void ChaseHorizontally() {
        foreach (SolarPylon pylon in _pylons.Values) {
          pylon.RotateHorizontally(HORIZONTAL_VELOCITY);
        }
      }
      public bool IsAlmostHorizontal() => Math.Abs(_witness.HorizontalPosition - 90) < 2;
      public void Reset() {
        foreach (SolarPylon pylon in _pylons.Values) {
          if (!pylon.HasReachedEndHorizontally(false)) {
            pylon.RotateHorizontally(-HORIZONTAL_VELOCITY * 5);
          } else {
            pylon.RotateHorizontally(0);
          }
          if (Math.Abs(pylon.VerticalPosition) > ALIGNMENT_CUTOFF) {
            pylon.RotateVertically(-Math.Sign(pylon.VerticalPosition) * VERTICAL_VELOCITY);
          } else {
            pylon.RotateVertically(0);
          }
        }
      }
      public void FixAlignment() {
        foreach (SolarPylon pylon in _pylons.Values) {
          float horizontalDiff = _witness.HorizontalPosition - pylon.HorizontalPosition;
          if (Math.Abs(horizontalDiff) > ALIGNMENT_CUTOFF) {
            pylon.RotateHorizontally(Math.Sign(horizontalDiff) * HORIZONTAL_VELOCITY);
          } else {
            pylon.RotateHorizontally(0);
          }
          float verticalDiff = _witness.VerticalPosition - pylon.VerticalPosition;
          if (Math.Abs(verticalDiff) > ALIGNMENT_CUTOFF) {
            pylon.RotateVertically(Math.Sign(verticalDiff) * VERTICAL_VELOCITY);
          } else {
            pylon.RotateVertically(0);
          }
        }
      }
      public bool WasLastTickRelevant() => _wasLastTickRelevant;
      // returns true if there has been a change of output
      public void Tick() {
        _wasLastTickRelevant = false;
        float curMaxOutput = MaxOutput;
        if (curMaxOutput != _lastOutputs[_historyIndex]) {
          _wasLastTickRelevant = true;
          _lastOutputs[++_historyIndex] = curMaxOutput;
        }
        if (curMaxOutput > _maxHistoricalOutput) {
          _maxHistoricalOutput = curMaxOutput;
        }
      }
      public void UpdateFromGrid() {
        foreach(SolarPylon pylon in _pylons.Values) {
          pylon.UpdateFromGrid();
        }
      }
      public float MaxOutput {
        get {
          return _pylons.Values.Sum(pylon => pylon.MaxOutput);
        }
      }
      public float MaxHistoricalOutput {
        get {
          return Math.Max(_maxHistoricalOutput, 0.001f);
        }
      }
      public float WitnessPosition {
        get {
          return _witness.HorizontalPosition;
        }
      }
      public void Serialize(Serializer serializer) {
        serializer.Serialize("maxSolarOutput", _maxHistoricalOutput);
        Marshaller marshaller = new Marshaller();
        marshaller.MarshallList(_pylons.Keys);
        serializer.Serialize("pylonKeys", marshaller.GetStorage());
      }
      public void Deserialize(Deserializer deserializer) {
        _maxHistoricalOutput = deserializer.GetAsFloat("maxSolarOutput");
        Unmarshaller unmarshaller = new Unmarshaller(deserializer.Get("pylonKeys"));
        string[] pylonKeys = unmarshaller.UnmarshallArrayOfString();
        foreach (string key in pylonKeys) {
          AddPylon(key);
        }
      }

      public String GetOutputHistory() {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < CircularContainer.CONTAINER_SIZE; ++i) {
          int idx = _historyIndex - i;
          if (i < CircularContainer.CONTAINER_SIZE - 1) {
            if (_lastOutputs[idx] < _lastOutputs[idx - 1]) {
              sb.Append(LCDColor.Red.ToChar());
            } else {
              sb.Append(LCDColor.Green.ToChar());
            }
            if (_lastOutputs.AverageLastN(2, idx) < _lastOutputs[idx - 2]) {
              sb.Append(LCDColor.Red.ToChar());
            } else {
              sb.Append(LCDColor.Green.ToChar());
            }
          }
          sb.Append(_lastOutputs[idx].ToString("F4")).Append("  ");
        }
        return sb.ToString();
      }

      public void AddPylon(string key) {
        if (!_pylons.ContainsKey(key)) {
          _pylons[key] = new SolarPylon(key);
        } else {
          ECHO("Not adding already present pylon.");
        }
      }

      class CircularContainer {
        public float this[int i] {
          get { return _container[ModuloForDebilo(i)]; }
          set { _container[ModuloForDebilo(i)] = value; }
        }
        public static int CONTAINER_SIZE = 8;
        public float AverageLastN(int number, int index) {
          float res = 0;
          for (int i = 0; i < number; ++i) {
            res += this[index - i];
          }
          return res / number;
        }
        private int ModuloForDebilo(int i) => ((i % CONTAINER_SIZE) + CONTAINER_SIZE) % CONTAINER_SIZE;
        float[] _container = new float[CONTAINER_SIZE];
      }
      CircularContainer _lastOutputs = new CircularContainer();
      int _historyIndex = 0;
      bool _wasLastTickRelevant = false;
      float _maxHistoricalOutput;
      SolarPylon _witness;
      Dictionary<string, SolarPylon> _pylons = new Dictionary<string, SolarPylon>();
    }
  }
}
