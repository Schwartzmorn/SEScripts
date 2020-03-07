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

    public class Display {
      public Vector2 SurfaceSize => _s.SurfaceSize;
      public Vector2 TextureSize => _s.TextureSize;

      readonly IMyTextSurface _s;
      readonly Vector2 _offset;
      readonly ColorScheme _scheme;

      readonly ShapeCollection _sprts;

      public class Frame : IDisposable {
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
