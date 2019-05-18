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
    // Circular buffer: because Space engineers does not like Queues...
    public class CircularBuffer<T>: IEnumerable<T> where T: class {
      private readonly List<T> _queue;
      private int _start = 0;
      public int Count { get; private set; }
      public int Capacity => _queue.Count;

      public CircularBuffer(int capacity) {
        _queue = new List<T>(Enumerable.Range(0, capacity).Select(_ => (T)null));
      }

      public CircularBuffer<T> Enqueue(T s) {
        int i = _incr(_start, Count);
        if (Count < Capacity) {
          ++Count;
        } else {
          _start = _incr(_start);
        }
        _queue[i] = s;
        return this;
      }

      public T Dequeue(bool dq = true) {
        if (Count == 0) {
          return null;
        }
        T res = _queue[_start];
        if (dq) {
          _start = _incr(_start);
          --Count;
        }
        return res;
      }

      public T Peek() => Dequeue(false);

      public void Clear() => Count = 0;

      public override string ToString() {
        var sb = new StringBuilder();
        int i = _start;
        for (int j = 0; j < Count; ++j) {
          sb.Append(_queue[i]);
          i = _incr(i);
        }
        return sb.ToString();
      }

      public IEnumerator<T> GetEnumerator() {
        int cI = _start;
        int eI = _incr(cI, Count);
        while (cI != eI) {
          yield return _queue[cI];
          cI = _incr(cI);
        }
      }

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      // Increment: I -could- have used modulos
      private int _incr(int i, int incr = 1) {
        i += incr;
        if (i >= Capacity) {
          i -= Capacity;
        }
        return i;
      }
    }
  }
}
