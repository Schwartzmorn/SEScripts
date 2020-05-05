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
    public class CircularBuffer<T> : IEnumerable<T> {
      readonly T[] queue;
      int start = 0;
      public int Count { get; private set; }
      int Capacity => this.queue.Count();
      /// <summary>Creates a buffer</summary>
      /// <param name="capacity">maximum capacity of the buffer</param>
      public CircularBuffer(int capacity) {
        this.queue = new T[capacity];
      }
      /// <summary>Adds an element to the queue</summary>
      /// <param name="s">Element to add</param>
      /// <returns>itself</returns>
      public CircularBuffer<T> Enqueue(T s) {
        this.queue[this.incr(this.start, this.Count)] = s;
        if (this.Count < this.Capacity) {
          ++this.Count;
        } else {
          this.start = this.incr(this.start);
        }
        return this;
      }
      /// <summary>Gets the first element and removes it</summary>
      /// <param name="dequeue">If true, the last element will be removed</param>
      /// <returns>the first inserted element</returns>
      public T Dequeue() {
        T res = this[0];
        if (!this.Empty) {
          this.start = this.incr(this.start);
          --this.Count;
        }
        return res;
      }
      /// <summary>Returns the first element, without removing it</summary>
      /// <returns>The first inserted element</returns>
      public T Peek() => this[0];
      /// <summary>Returns whether the queue is empty</summary>
      public bool Empty => this.Count == 0;
      /// <summary>Empties the buffer</summary>
      public void Clear() => this.Count = 0;
      /// <summary>
      /// returns the element at the given index. If outside of bounds, it just returns the default value.
      /// Index can be negative, in which case it counts from the last object inserted, -1 being the last
      /// </summary>
      /// <param name="i">index of the object, 0 being the first object inserted, -1 the last</param>
      /// <returns>the object at the given index</returns>
      public T this[int i] => (i >= this.Count || -i > this.Count)
              ? default(T)
              : this.queue[this.incr(this.start, i < 0 ? this.Count + i : i)];
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
        return i >= this.Capacity ? i - this.Capacity : i;
      }
    }
  }
}
