using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
partial class Program {
  public class ColorScheme {
    public readonly Color Dark = new Color(0, 39, 15);
    public readonly Color MedDark = new Color(9, 102, 21);
    public readonly Color MedLight = new Color(18, 165, 27);
    public readonly Color Light = new Color(27, 228, 33);
    public Color GetColor(string nm) {
      string s = nm.ToLower();
      return s == "dark" ? Dark : s == "meddark" ? MedDark : s == "medlight" ? MedLight : Light;
    }
  }

  // Helper class to define shapes. Positioned and rotated around the top left corner
  public struct Shape {
    public readonly string Sprite;
    public readonly Color Col;
    public readonly Vector2 Pos;
    public readonly Vector2 Size;
    public readonly float Rot;

    public Shape(string sprite, Color col, Vector2 pos, Vector2 size, float rot = 0f) {
      Sprite = sprite;
      Col = col;
      Pos = pos;
      Size = size;
      Rot = rot;
    }
  }

  public class ShapeCollection {
    static readonly char[] SEPLN = new char[] { '\n' };
    static readonly char[] SEP = new char[] { ':' };
    static readonly char[] SEP_VEC = new char[] { ',' };

    Vector2 _offset;
    ColorScheme _scheme;

    readonly Dictionary<string, List<MySprite>> _sprts = new Dictionary<string, List<MySprite>>();

    public ShapeCollection(Vector2? offset = null, ColorScheme scheme = null) {
      _offset = offset ?? Vector2.Zero;
      _scheme = scheme ?? new ColorScheme();
    }

    public List<MySprite> Get(string nm) => _sprts[nm];

    public void Parse(string data) {
      List<MySprite> sprts = null;
      int count = 0;
      foreach(string l in data.Split(SEPLN).Select(l => l.Trim())) {
        ++count;
        if (l.StartsWith(";") || l == "") continue;
        if (l.StartsWith("=")) {
          sprts = new List<MySprite>();
          _sprts.Add(l.Substring(1), sprts);
        } else {
          _throwIf(sprts == null, count, "no collection name");
          var vals = l.Split(SEP);
          _throwIf(vals.Count() != 5, count, "not enough or too many values");
          try {
            var cs = vals[1].Split(SEP_VEC);
            var col = cs.Count() == 3 ? new Color(int.Parse(cs[0]), int.Parse(cs[1]), int.Parse(cs[2])) : _scheme.GetColor(cs[0].Trim());
            sprts.Add(_toSprt(vals[0].Trim(), col, _parse(vals[2]), _parse(vals[3]), float.Parse(vals[4])));
          } catch {
            _throwIf(true, count, "could not parse");
          }
        }
      }
    }

    void _throwIf(bool t, int i, string msg) { if (t) throw new InvalidOperationException($"Error at line {i}: {msg}"); }

    Vector2 _parse(string s) {
      var ent = s.Split(SEP_VEC);
      return new Vector2(float.Parse(ent[0]), float.Parse(ent[1]));
    }

    MySprite _toSprt(string s, Color col, Vector2 pos, Vector2 size, float rot) {
      var realPos = new Vector2(Math.Abs(size.X) / 2, Math.Abs(size.Y) / 2);
      realPos.Rotate(rot);
      return new MySprite(SpriteType.TEXTURE, s, realPos + pos + _offset, size, col, rotation: rot);
    }
  }


  public class Display {
    public Vector2 SurfaceSize => _s.SurfaceSize;
    public Vector2 TextureSize => _s.TextureSize;

    readonly IMyTextSurface _s;
    readonly Vector2 _offset;
    readonly ColorScheme _scheme;

    readonly ShapeCollection _sprts;

    public class Frame: IDisposable {
      readonly MySpriteDrawFrame _f;
      readonly Display _d;

      public Frame(Display display) {
        _d = display;
        _f = _d._s.DrawFrame();
        _f.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", _d._s.TextureSize / 2, _d._s.TextureSize, _d._scheme.Dark));
      }

      public void Dispose() {
        _f.Add(new MySprite(SpriteType.TEXTURE, "Grid", _d._s.TextureSize / 2, _d._s.TextureSize * 2, _d._scheme.Light));
        _f.Dispose();
      }

      public void Draw(Shape shape) {
        var realPos = new Vector2(Math.Abs(shape.Size.X) / 2, Math.Abs(shape.Size.Y) / 2);
        realPos.Rotate(shape.Rot);
        _f.Add(new MySprite(SpriteType.TEXTURE, shape.Sprite, realPos + shape.Pos + _d._offset, shape.Size, shape.Col, rotation: shape.Rot));
      }

      public void DrawTxt(string txt, Vector2 pos, Color? color = null, float scale = 1, TextAlignment al = TextAlignment.CENTER) {
        var sprite = MySprite.CreateText(txt, "Monospace", color ?? _d._scheme.Light, scale, al);
        sprite.Position = pos + _d._offset;
        _f.Add(sprite);
      }

      public void DrawCollection(string name) => _f.AddRange(_d._sprts.Get(name));

      // translate, then rotate
      public void DrawCollectionTform(string name, Vector2? translation = null, Vector2? centerOfRotation = null, float rot = 0f, Color? col = null) => _f.AddRange(_d._sprts.Get(name).Select(sprite => {
        Vector2 realPos = (sprite.Position ?? Vector2.Zero) + (translation ?? Vector2.Zero);
        Vector2 realCenterOfRot = _d._offset + (centerOfRotation ?? Vector2.Zero);
        Vector2 offset = realPos - realCenterOfRot;
        offset.Rotate(rot);
        realPos = realCenterOfRot + offset;
        return new MySprite(SpriteType.TEXTURE, sprite.Data, realPos, sprite.Size, col ?? sprite.Color, rotation: sprite.RotationOrScale + rot);
      }));
    }

    public Display(IMyTextSurface s, Vector2? offset = null, ColorScheme scheme = null, ShapeCollection sprts = null) {
      s.ContentType = ContentType.SCRIPT;
      s.Script = "";
      _s = s;
      _offset = offset ?? Vector2.Zero;
      _scheme = scheme ?? new ColorScheme();
      _sprts = sprts;
    }

    public Frame DrawFrame() => new Frame(this);
  }
}
}
