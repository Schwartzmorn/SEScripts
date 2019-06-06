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
public class CircBuf<T>: IEnumerable<T> where T: class {
readonly List<T> _q;
int _s=0;
public int Count { get; private set; }
int Cap => _q.Count;
public CircBuf(int capacity) {
_q=new List<T>(Enumerable.Range(0, capacity).Select(_ => (T)null));
}
public CircBuf<T> Enqueue(T s) {
int i=_incr(_s, Count);
if (Count < Cap)
  ++Count;
else
  _s=_incr(_s);
_q[i]=s;
return this;
}
public T Dequeue(bool dq = true) {
if (Count==0)
  return null;
T res=_q[_s];
if (dq) {
  _s=_incr(_s);
  --Count;
}
return res;
}
public T Peek() => Dequeue(false);
public void Clr() => Count=0;
public override string ToString() {
var sb = new StringBuilder();
int i = _s;
for (int j=0; j < Count; ++j) {
  sb.Append(_q[i]);
  i = _incr(i);
}
return sb.ToString();
}
public IEnumerator<T> GetEnumerator() {
int cI=_s, eI=_incr(cI, Count);
while (cI!=eI) {
  yield return _q[cI];
  cI=_incr(cI);
}
}
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
int _incr(int i, int incr=1) {
i+=incr;
if (i>=Cap)
  i-=Cap;
return i;
}
}
}
}
