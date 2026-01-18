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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>FIFO container of fixed max capacity. Once the capacity is reached, any insertion replaces the first inserted</summary>
    /// <typeparam name="T">Type of object held in the buffer</typeparam>
    public class CircularBuffer<T> : IEnumerable<T>
    {
      readonly T[] _queue;
      int _start = 0;
      public int Count { get; private set; }
      int Capacity => _queue.Length;

      /// <summary>Creates a buffer</summary>
      /// <param name="capacity">maximum capacity of the buffer</param>
      public CircularBuffer(int capacity)
      {
        _queue = new T[capacity];
      }

      /// <summary>Adds an element to the queue</summary>
      /// <param name="s">Element to add</param>
      /// <returns>itself</returns>
      public CircularBuffer<T> Enqueue(T s)
      {
        _queue[_incr(_start, Count)] = s;
        if (Count < Capacity)
        {
          ++Count;
        }
        else
        {
          _start = _incr(_start);
        }
        return this;
      }

      /// <summary>Gets the first element and removes it</summary>
      /// <param name="dequeue">If true, the last element will be removed</param>
      /// <returns>the first inserted element</returns>
      public T Dequeue()
      {
        T res = this[0];
        if (!Empty)
        {
          _start = _incr(_start);
          --Count;
        }
        return res;
      }

      /// <summary>Returns the first element, without removing it</summary>
      /// <returns>The first inserted element</returns>
      public T Peek() => this[0];

      /// <summary>Returns whether the queue is empty</summary>
      public bool Empty => Count == 0;

      /// <summary>Empties the buffer</summary>
      public void Clear() => Count = 0;

      /// <summary>
      /// returns the element at the given index. If outside of bounds, it just returns the default value.
      /// Index can be negative, in which case it counts from the last object inserted, -1 being the last
      /// </summary>
      /// <param name="i">index of the object, 0 being the first object inserted, -1 the last</param>
      /// <returns>the object at the given index</returns>
      public T this[int i] => (i >= Count || -i > Count)
              ? default(T)
              : _queue[_incr(_start, i < 0 ? Count + i : i)];

      /// <summary>Returns an enumerator on all the element in the buffer, from the first inserted to the last</summary>
      /// <returns>The enumerator</returns>
      public IEnumerator<T> GetEnumerator()
      {
        int i = 0;
        while (i != Count)
        {
          yield return _queue[_incr(_start, i++)];
        }
      }
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
      int _incr(int i, int incr = 1)
      {
        i += incr;
        return i >= Capacity ? i - Capacity : i;
      }
    }
  }
}
