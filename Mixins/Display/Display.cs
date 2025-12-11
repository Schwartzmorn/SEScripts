using Sandbox.ModAPI.Ingame;
using System;
using System.Linq;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public class ColorScheme
    {
      public readonly Color Dark = new Color(0, 39, 15);
      public readonly Color MedDark = new Color(9, 102, 21);
      public readonly Color MedLight = new Color(18, 165, 27);
      public readonly Color Light = new Color(27, 228, 33);
      public Color GetColor(string nm)
      {
        string s = nm.ToLower();
        return s == "dark" ? Dark : s == "meddark" ? MedDark : s == "medlight" ? MedLight : Light;
      }
    }

    /// <summary>Class that wraps a <see cref="IMyTextSurface"/> to be more convenient</summary>
    public class Display
    {
      public Vector2 SurfaceSize => _surface.SurfaceSize;
      public Vector2 TextureSize => _surface.TextureSize;

      readonly IMyTextSurface _surface;
      readonly Vector2 _offset;
      readonly float _scale;
      readonly ColorScheme _scheme;
      readonly ShapeCollections _sprites;

      /// <summary><see cref="IDisposable"/> wrapper around a <see cref="MySpriteDrawFrame"/> with a more convenient interface</summary>
      public class Frame : IDisposable
      {
        public readonly MySpriteDrawFrame _f;
        readonly Display _d;
        /// <summary>Creates a frame for a <see cref="Display"/>. Inherits its offset and <see cref="ShapeCollections"/></summary>
        /// <param name="display">Display on which to draw the frame</param>
        public Frame(Display display)
        {
          _d = display;
          _f = _d._surface.DrawFrame();
          _f.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", _d._surface.TextureSize / 2, _d._surface.TextureSize, _d._scheme.Dark));
        }
        /// <summary>Adds some texture and commits the frame</summary>
        public void Dispose()
        {
          _f.Add(new MySprite(SpriteType.TEXTURE, "Grid", _d._surface.TextureSize / 2, _d._surface.TextureSize * 2, _d._scheme.Light));
          _f.Dispose();
        }
        /// <summary>Draws a <see cref="Shape"/> on the frame</summary>
        /// <param name="shape">Shape to draw</param>
        public void Draw(Shape shape)
        {
          Vector2 realSize = shape.Size * _d._scale;
          Vector2 realPos = shape.Centered ? Vector2.Zero : (new Vector2(Math.Abs(realSize.X), Math.Abs(realSize.Y)) / 2);
          realPos += shape.Position * _d._scale;
          _f.Add(new MySprite(SpriteType.TEXTURE, shape.Sprite, realPos + _d._offset, realSize, shape.Color, rotation: shape.Rotation));
        }
        /// <summary>Draws some text on the frame</summary>
        /// <param name="text">Text to draw</param>
        /// <param name="position">Where to draw the text, in conjunction with <paramref name="alignment"/>. Affected by the display's scale</param>
        /// <param name="color">Color of the text, by default it will be the <see cref="_d"/>'s <see cref="ColorScheme.Light"/> color</param>
        /// <param name="scale">Scale of the text. Compounded with the display's scale</param>
        /// <param name="alignment">Alignment</param>
        public void DrawText(string text, Vector2 position, Color? color = null, float scale = 1, TextAlignment alignment = TextAlignment.CENTER)
        {
          var sprite = MySprite.CreateText(text, "Monospace", color ?? _d._scheme.Light, scale * _d._scale, alignment);
          sprite.Position = (position * _d._scale) + _d._offset;
          _f.Add(sprite);
        }
        /// <summary>Draws a collection of sprites from the <see cref="_d"/>'s <see cref="ShapeCollections"/>. The rotation is made before the translation around 0, 0</summary>
        /// <param name="name">Name of the collection to draw</param>
        /// <param name="translation">By how much the shape will be offset. Affected by the display's scale</param>
        /// <param name="rotation">By how much the collection will be rotated. It will always be rotated around the 0, 0 point of the collection</param>
        /// <param name="color">If not null, will override the color from the collection</param>
        public void DrawCollection(string name, Vector2? translation = null, float rotation = 0f, Color? color = null) => _f.AddRange(_d._sprites.Get(name).Select(sprite =>
        {
          Vector2 size = (sprite.Size ?? Vector2.One) * _d._scale;
          Vector2 realPos = (sprite.Position ?? Vector2.Zero) * _d._scale;
          realPos.Rotate(rotation);
          realPos += (translation ?? Vector2.Zero) * _d._scale;
          return new MySprite(SpriteType.TEXTURE, sprite.Data, realPos + _d._offset, size, color ?? sprite.Color, rotation: sprite.RotationOrScale + rotation);
        }));
      }
      /// <summary>Creates a <see cref="Display"/></summary>
      /// <param name="surface">Surface to wrap</param>
      /// <param name="offset">Everything drawn on this <see cref="Display"/> will be offset by this</param>
      /// <param name="scale">Everything drawn on this <see cref="Display"/> will be scaled by this</param>
      /// <param name="scheme">Gives the base colors of the display</param>
      /// <param name="sprites">The collection of shapes that can be easility drawn on the surface</param>
      public Display(IMyTextSurface surface, Vector2? offset = null, float scale = 1f, ColorScheme scheme = null, ShapeCollections sprites = null)
      {
        surface.ContentType = ContentType.SCRIPT;
        surface.Script = "";
        _surface = surface;
        _offset = offset ?? Vector2.Zero;
        _scale = scale;
        _scheme = scheme ?? new ColorScheme();
        _sprites = sprites;
      }
      /// <summary>Creates and return a new <see cref="Frame"/></summary>
      /// <returns>The frame</returns>
      public Frame DrawFrame() => new Frame(this);
    }
  }
}
