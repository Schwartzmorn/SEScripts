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

namespace IngameScript
{
  partial class Program
  {
    /// <summary>Helper class to define shapes. Positioned and rotated around the top left corner</summary>
    public struct Shape
    {
      public readonly bool Centered;
      public readonly Color Color;
      public readonly Vector2 Position;
      public readonly float Rotation;
      public readonly string Sprite;
      public readonly Vector2 Size;

      public Shape(string sprite, Color color, Vector2 position, Vector2 size, float rotation = 0f, bool centered = false)
      {
        Sprite = sprite;
        Color = color;
        Position = position;
        Size = size;
        Rotation = rotation;
        Centered = centered;
      }
    }
    /// <summary>Class to hold lists of sprites that can then easily be reused</summary>
    public class ShapeCollections
    {
      static readonly char[] SEPLN = new char[] { '\n' };
      static readonly char[] SEP = new char[] { ':' };
      static readonly char[] SEP_VEC = new char[] { ',' };

      readonly ColorScheme _scheme;
      readonly Dictionary<string, List<MySprite>> _sprites = new Dictionary<string, List<MySprite>>();

      /// <summary>Creates a new collection</summary>
      /// <param name="scheme">Scheme to be used for the colors of the sprites</param>
      public ShapeCollections(ColorScheme scheme = null)
      {
        _scheme = scheme ?? new ColorScheme();
      }
      /// <summary>Returns the list of sprites associated with the name</summary>
      /// <param name="nm">Name of the list of sprites</param>
      /// <returns>the list of sprites</returns>
      public List<MySprite> Get(string nm) => _sprites[nm];
      /// <summary>
      /// Parses the string to get the list of sprites it contains. The string is parsed line by line with the following rules. Throws if it cannot parse the string.
      /// <list type="bullet">
      /// <item>Empty lines or lines that start with a ';' are ignored</item>
      /// <item>Lines that start with a '=' notify the start of a new list, followed by its name</item>
      /// <item>Values a vector must be separated by ','</item>
      /// <item>other lines must contain a sprite, that must contain the following values separated by a ':', in this order:
      /// <list type="number">
      ///   <item>name of the shape for <see cref="MySprite"/></item>
      ///   <item>color, either as RGB int vector or the shade of a <see cref="ColorScheme"/></item>
      ///   <item>the position, as a two components vector</item>
      ///   <item>the size, as a two components vector</item>
      ///   <item>optionally, the rotation</item>
      ///   <item>optionally, "center" if the position and rotation should be based on the center of the sprite instead of the top left</item>
      /// </list>
      /// </item>
      /// </list>
      /// </summary>
      /// <param name="data">string to parse</param>
      public void Parse(string data)
      {
        List<MySprite> sprts = null;
        int count = 0;
        foreach (string line in data.Split(SEPLN).Select(l => l.Trim()))
        {
          ++count;
          if (line.StartsWith(";") || line == "")
          {
            continue;
          }
          if (line.StartsWith("="))
          {
            sprts = new List<MySprite>();
            _sprites.Add(line.Substring(1).Trim(), sprts);
          }
          else
          {
            _throwIf(sprts == null, count, "found sprite before collection name");
            string[] vals = line.Split(SEP);
            _throwIf(vals.Count() < 4 || vals.Count() > 6, count, "not enough or too many values");
            Color color = _parseColor(vals[1], count);
            float rotation = 0;
            if (vals.Count() > 4)
            {
              _throwIf(!float.TryParse(vals[4], out rotation), count, "could not parse rotation");
            }
            bool isCentered = (vals.Count() == 6) && vals[5].Trim().ToLower().Contains("center");
            sprts.Add(_toSprt(vals[0].Trim(), color, _parseVector2(vals[2], count, "position"), _parseVector2(vals[3], count, "size"), rotation, isCentered));
          }
        }
      }

      void _throwIf(bool shouldThrow, int line, string msg)
      {
        if (shouldThrow)
        {
          throw new InvalidOperationException($"Error at line {line}: {msg}");
        }
      }

      Color _parseColor(string s, int line)
      {
        string[] cs = s.Split(SEP_VEC);
        if (cs.Count() == 1)
        {
          return _scheme.GetColor(cs[0].Trim());
        }
        else if (cs.Count() == 3)
        {
          int r, g = 0, b = 0;
          bool res = int.TryParse(cs[0], out r) && int.TryParse(cs[1], out g) && int.TryParse(cs[2], out b);
          _throwIf(!res, line, "could not parse rgb color");
          return new Color(r, g, b);
        }
        else
        {
          _throwIf(true, line, "not enough or too many values for the color");
          return new Color();
        }
      }

      Vector2 _parseVector2(string s, int line, string name)
      {
        string[] ent = s.Split(SEP_VEC);
        if (ent.Count() != 2)
        {
          _throwIf(true, line, $"not enough or too many values for the {name}");
        }
        float x, y = 0;
        bool res = float.TryParse(ent[0], out x) && float.TryParse(ent[1], out y);
        _throwIf(!res, line, $"could not parse {name}");
        return new Vector2(x, y);
      }

      MySprite _toSprt(string s, Color col, Vector2 pos, Vector2 size, float rot, bool isCentered)
      {
        Vector2 realPos = pos + (isCentered ? Vector2.Zero : (new Vector2(Math.Abs(size.X), Math.Abs(size.Y)) / 2));
        return new MySprite(SpriteType.TEXTURE, s, realPos, size, col, rotation: rot);
      }
    }
  }
}
