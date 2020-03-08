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
    /// <summary>FIFO container of fixed max capacity. Once the capacity is reached, any insertion replaces the first inserted</summary>
    /// <typeparam name="T">Type of object held in the buffer</typeparam>
    public class CircularBuffer<T> : IEnumerable<T> where T : class {
      readonly List<T> queue;
      int start = 0;
      public int Count { get; private set; }
      int Capacity => this.queue.Count;
      /// <summary>Creates a buffer</summary>
      /// <param name="capacity">maximum capacity of the buffer</param>
      public CircularBuffer(int capacity) {
        this.queue = new List<T>(Enumerable.Range(0, capacity).Select(_ => (T)null));
      }
      /// <summary>Adds an element to the queue</summary>
      /// <param name="s">Element to add</param>
      /// <returns>itself</returns>
      public CircularBuffer<T> Enqueue(T s) {
        int i = this.incr(this.start, this.Count);
        if (this.Count < this.Capacity) {
          ++this.Count;
        } else {
          this.start = this.incr(this.start);
        }

        this.queue[i] = s;
        return this;
      }
      /// <summary>Gets the first element, and optionally removes it</summary>
      /// <param name="dequeue">If true, the last element will be removed</param>
      /// <returns>the first inserted element</returns>
      public T Dequeue(bool dequeue = true) {
        if (this.Count == 0) {
          return null;
        }

        T res = this.queue[this.start];
        if (dequeue) {
          this.start = this.incr(this.start);
          --this.Count;
        }
        return res;
      }
      /// <summary>Returns the first element, without removing it</summary>
      /// <returns>The first inserted element</returns>
      public T Peek() => this.Dequeue(false);
      /// <summary>Empties the buffer</summary>
      public void Clear() => this.Count = 0;
      public override string ToString() {
        var sb = new StringBuilder();
        int i = this.start;
        for (int j = 0; j < this.Count; ++j) {
          sb.Append(this.queue[i]);
          i = this.incr(i);
        }
        return sb.ToString();
      }
      /// <summary>Returns an enumerator on all the element in the buffer, from the first inserted to the last</summary>
      /// <returns>The enumerator</returns>
      public IEnumerator<T> GetEnumerator() {
        int i = 0;
        while (i != this.Count) {
          yield return this.queue[this.incr(this.start, i++)];
        }
      }
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
      int incr(int i, int incr = 1) {
        i += incr;
        if (i >= this.Capacity) {
          i -= this.Capacity;
        }

        return i;
      }
    }
  }
}
