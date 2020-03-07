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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program {
    /// <summary>Helper class to define shapes. Positioned and rotated around the top left corner</summary>
    public struct Shape {
      public readonly string Sprite;
      public readonly Color Col;
      public readonly Vector2 Pos;
      public readonly Vector2 Size;
      public readonly float Rot;

      public Shape(string sprite, Color col, Vector2 pos, Vector2 size, float rot = 0f) {
        this.Sprite = sprite;
        this.Col = col;
        this.Pos = pos;
        this.Size = size;
        this.Rot = rot;
      }
    }
    /// <summary>Class to hold lists of sprites that can then easily be reused</summary>
    public class ShapeCollection {
      static readonly char[] SEPLN = new char[] { '\n' };
      static readonly char[] SEP = new char[] { ':' };
      static readonly char[] SEP_VEC = new char[] { ',' };

      readonly Vector2 offset;
      readonly ColorScheme scheme;
      readonly Dictionary<string, List<MySprite>> sprites = new Dictionary<string, List<MySprite>>();

      /// <summary>Creates a new collection</summary>
      /// <param name="offset">Offset the position of all the sprites by this amount</param>
      /// <param name="scheme">Determines the colors of the sprites</param>
      public ShapeCollection(Vector2? offset = null, ColorScheme scheme = null) {
        this.offset = offset ?? Vector2.Zero;
        this.scheme = scheme ?? new ColorScheme();
      }
      /// <summary>Returns the list of sprites associated with the name</summary>
      /// <param name="nm">Name of the list of sprites</param>
      /// <returns>the list of sprites</returns>
      public List<MySprite> Get(string nm) => this.sprites[nm];
      /// <summary>
      /// Parses the string to get the list of sprites it contains. The string is parsed line by line with the following rules. Throws if it cannot parse the string.
      /// <list type="bullet">
      /// <item>Empty lines or lines that start with a ';' are ignored</item>
      /// <item>Lines that start with a '=' notify the start of a new list, followed by its name</item>
      /// <item>Values a vector must be separated by ','</item>
      /// <item>other lines must contain a sprite, that must contain the following values separated by a ':', in this order:
      /// <list type="number">
      ///   <item>name of the shape for <see cref="MySprite"/></item>
      ///   <item>color, either as RGB vector or the shade of a <see cref="ColorScheme"/></item>
      ///   <item>the position, as a two components vector</item>
      ///   <item>the size, as a two components vector</item>
      ///   <item>the rotation</item>
      /// </list>
      /// </item>
      /// </list>
      /// </summary>
      /// <param name="data">string to parse</param>
      public void Parse(string data) {
        List<MySprite> sprts = null;
        int count = 0;
        foreach (string l in data.Split(SEPLN).Select(l => l.Trim())) {
          ++count;
          if (l.StartsWith(";") || l == "") {
            continue;
          } 
          if (l.StartsWith("=")) {
            sprts = new List<MySprite>();
            this.sprites.Add(l.Substring(1), sprts);
          } else {
            this.throwIf(sprts == null, count, "no collection name");
            string[] vals = l.Split(SEP);
            this.throwIf(vals.Count() != 5, count, "not enough or too many values");
            try {
              string[] cs = vals[1].Split(SEP_VEC);
              var col = cs.Count() == 3 ? new Color(int.Parse(cs[0]), int.Parse(cs[1]), int.Parse(cs[2])) : this.scheme.GetColor(cs[0].Trim());
              sprts.Add(this.toSprt(vals[0].Trim(), col, this.parse(vals[2]), this.parse(vals[3]), float.Parse(vals[4])));
            } catch {
              this.throwIf(true, count, "could not parse");
            }
          }
        }
      }

      void throwIf(bool t, int i, string msg) {
        if (t) {
          throw new InvalidOperationException($"Error at line {i}: {msg}"); 
        }
      }

      Vector2 parse(string s) {
        string[] ent = s.Split(SEP_VEC);
        return new Vector2(float.Parse(ent[0]), float.Parse(ent[1]));
      }

      MySprite toSprt(string s, Color col, Vector2 pos, Vector2 size, float rot) {
        var realPos = new Vector2(Math.Abs(size.X) / 2, Math.Abs(size.Y) / 2);
        realPos.Rotate(rot);
        return new MySprite(SpriteType.TEXTURE, s, realPos + pos + this.offset, size, col, rotation: rot);
      }
    }
  }
}
