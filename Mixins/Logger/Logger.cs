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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
partial class Program {
public static void Log(string s) => Logger.I.Log(s);
class Logger {
public static Logger I { get; private set; }
static readonly List<Logger> IS=new List<Logger>();
Action<string> _echo;
readonly CircBuf<string> _msgs;
bool _c;
readonly IMyTextSurface _s;
public static void SetupGlobalInstance(Logger l, Action<string> e) {
if (I!=null)
  I._echo=null;
l._echo=e;
I=l;
}
public Logger(IMyTextSurface s, float size=0.5f, Color? col=null, Color? bgdCol=null) {
int nMsgs=1;
if (s!=null) {
  s.TextPadding=0;
  s.ContentType=(ContentType)1;
  s.Alignment=(TextAlignment)0;
  s.Font="Monospace";
  s.FontSize=size;
  s.FontColor=col??Color.White;
  s.BackgroundColor=bgdCol??Color.Black;
  var sb=new StringBuilder("G");
  nMsgs=(int)((_mult(s)*s.SurfaceSize.Y/s.MeasureStringInPixels(sb, s.Font, s.FontSize).Y)+0.1f);
  _s=s;
}
_msgs = new CircBuf<string>(nMsgs);
if(IS.Count==0)
  Schedule(() => IS.ForEach(l => l._f()));
IS.Add(this);
}
public void Log(string log) {
_c=true;
var logs=log.Split('\n');
foreach(var l in logs)
  _msgs.Enqueue(l+'\n');
_echo?.Invoke(log);
}
void _f() {
if (_c) {
  _c=false;
  _s?.WriteText(_msgs.ToString());
}
}
static float _mult(IMyTextSurface s) {
var sy=s.SurfaceSize.Y;
if(s is IMyTerminalBlock) {//txt panel
  var t=(s as IMyTerminalBlock).BlockDefinition.SubtypeId;
  if(t.Contains("Corner"))
    return t.Contains("Large") ? t.Contains("Flat") ? 0.168f : 0.146f : t.Contains("Flat") ? 0.302f : 0.260f;
} else {
  var nm=s.DisplayName;
  if(nm=="Large Display")
    return sy<200 ? 4 : sy<300 ? 2 : 1;//flt sit,small prog blk
  else if(nm=="Keyboard")
    return (sy<110) ? 4 : 2;//fter cpit and small prog blk
  else if(nm.Contains("Screen"))
    return (sy<100 && nm.Contains("Top") && (nm.Contains("Left") || nm.Contains("Right"))) ? 4 : 2;//fter cpit top left right screens
}
return 1;
}
}
}
}
