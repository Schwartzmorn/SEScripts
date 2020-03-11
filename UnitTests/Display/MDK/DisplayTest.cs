using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VRageMath;
using VRage.Game.GUI.TextPanel;

namespace IngameScript.MDK {
  class DisplayTest {
    readonly Program.ColorScheme scheme = new Program.ColorScheme();
    Mockups.Base.MockTextSurface surface;
    private void checkVector(Vector2 expected, Vector2 actual) {
      Vector2 diff = expected - actual;
      Assert.IsTrue(Math.Abs(diff.X) < 0.001, $"actual: {actual} expected: {expected}");
      Assert.IsTrue(Math.Abs(diff.Y) < 0.001, $"actual: {actual} expected: {expected}");
    }
    public void BeforeEach() {
      this.surface = new Mockups.Base.MockTextSurface(new Vector2(100, 100), new Vector2(100, 100));
    }
    public void DrawShape() {
      var display = new Program.Display(this.surface, offset: new Vector2(10f, 30f), scale: 2);

      var circle = new Program.Shape("Circle", Color.White, new Vector2(50, 50), new Vector2(30, 30), centered: true);
      var square = new Program.Shape("SquareSimple", Color.White, new Vector2(50, 50), new Vector2(30, 30));
      var cross = new Program.Shape("Cross", Color.White, new Vector2(20, 20), new Vector2(30, 10), MathHelper.PiOver2);

      using (Program.Display.Frame frame = display.DrawFrame()) {
        frame.Draw(circle);
        frame.Draw(square);
        frame.Draw(cross);
      }

      var sprites = new List<MySprite>(this.surface.SpriteBuffer);

      Assert.AreEqual(5, sprites.Count, "Background + grid pattern + whatever other sprites were added");

      MySprite sprite = sprites[1];

      Assert.AreEqual("Circle", sprite.Data);
      Assert.AreEqual(Color.White, sprite.Color);
      Assert.AreEqual(new Vector2(110f, 130f), sprite.Position);
      Assert.AreEqual(new Vector2(60f, 60f), sprite.Size);
      Assert.AreEqual(0, sprite.RotationOrScale);

      sprite = sprites[2];

      Assert.AreEqual("SquareSimple", sprite.Data);
      Assert.AreEqual(new Vector2(140f, 160f), sprite.Position);
      Assert.AreEqual(new Vector2(60f, 60f), sprite.Size);
      Assert.AreEqual(0, sprite.RotationOrScale);

      sprite = sprites[3];

      Assert.AreEqual("Cross", sprite.Data);
      Assert.AreEqual(new Vector2(80f, 80f), sprite.Position);
      Assert.AreEqual(new Vector2(60f, 20f), sprite.Size);
      Assert.AreEqual(MathHelper.PiOver2, sprite.RotationOrScale);
    }
    public void DrawText() {
      var display = new Program.Display(this.surface, offset: new Vector2(10f, 30f), scale: 2);

      using (Program.Display.Frame frame = display.DrawFrame()) {
        frame.DrawText("Some text 1", new Vector2(20, 20), scale: 0.5f);
        frame.DrawText("Some text 2", new Vector2(50, 50), this.scheme.MedLight, 2, TextAlignment.LEFT);
      }

      var sprites = new List<MySprite>(this.surface.SpriteBuffer);

      Assert.AreEqual(4, sprites.Count, "Background + grid pattern + whatever other sprites were added");

      MySprite sprite = sprites[1];

      Assert.AreEqual("Some text 1", sprite.Data);
      Assert.AreEqual("Monospace", sprite.FontId);
      Assert.AreEqual(new Vector2(50f, 70f), sprite.Position);
      Assert.AreEqual(1, sprite.RotationOrScale);
      Assert.AreEqual(TextAlignment.CENTER, sprite.Alignment);

      sprite = sprites[2];

      Assert.AreEqual("Some text 2", sprite.Data);
      Assert.AreEqual("Monospace", sprite.FontId);
      Assert.AreEqual(new Vector2(110f, 130f), sprite.Position);
      Assert.AreEqual(4, sprite.RotationOrScale);
      Assert.AreEqual(TextAlignment.LEFT, sprite.Alignment);
    }

    public void DrawCollectionNoTransform() {
      var collection = new Program.ShapeCollections();

      collection.Parse(@"
      =Collection
      Triangle: light: 30,40: 10,10: 0: center
      Circle  : dark : 10,30:  5, 5: 1.570796: center
      ");

      var display = new Program.Display(this.surface, offset: new Vector2(10f, 50f), scale: 0.5f, sprites: collection);

      using (Program.Display.Frame frame = display.DrawFrame()) {
        frame.DrawCollection("Collection");
      }

      var sprites = new List<MySprite>(this.surface.SpriteBuffer);

      Assert.AreEqual(4, sprites.Count, "Background + grid pattern + whatever other sprites were added");

      MySprite sprite = sprites[1];

      Assert.AreEqual("Triangle", sprite.Data);
      Assert.AreEqual(this.scheme.Light, sprite.Color);
      Assert.AreEqual(new Vector2(25f, 70f), sprite.Position);
      Assert.AreEqual(new Vector2(5f, 5f), sprite.Size);
      Assert.AreEqual(0, sprite.RotationOrScale);

      sprite = sprites[2];

      Assert.AreEqual("Circle", sprite.Data);
      Assert.AreEqual(this.scheme.Dark, sprite.Color);
      Assert.AreEqual(new Vector2(15f, 65f), sprite.Position);
      Assert.AreEqual(new Vector2(2.5f, 2.5f), sprite.Size);
      Assert.AreEqual(MathHelper.PiOver2, sprite.RotationOrScale);
    }

    public void DrawCollectionTransform() {
      var collection = new Program.ShapeCollections();

      collection.Parse(@"
      =Collection
      Triangle: light: 30,40: 10,10: 0: center
      Circle  : dark : 10,30:  5, 5: 1.570796326794896619: center
      ");

      var display = new Program.Display(this.surface, offset: new Vector2(10f, 50f), scale: 0.5f, sprites: collection);

      using (Program.Display.Frame frame = display.DrawFrame()) {
        frame.DrawCollection("Collection", new Vector2(10, 20), Convert.ToSingle(Math.PI) / 2, this.scheme.MedDark);
      }

      var sprites = new List<MySprite>(this.surface.SpriteBuffer);

      Assert.AreEqual(4, sprites.Count, "Background + grid pattern + whatever other sprites were added");

      MySprite sprite = sprites[1];

      Assert.AreEqual("Triangle", sprite.Data);
      Assert.AreEqual(this.scheme.MedDark, sprite.Color);
      this.checkVector(new Vector2(-5, 75), sprite.Position.Value);
      Assert.AreEqual(Convert.ToSingle(Math.PI) / 2, sprite.RotationOrScale);

      sprite = sprites[2];

      Assert.AreEqual("Circle", sprite.Data);
      Assert.AreEqual(this.scheme.MedDark, sprite.Color);
      Assert.AreEqual(new Vector2(0, 65), sprite.Position);
      Assert.AreEqual(Convert.ToSingle(Math.PI), sprite.RotationOrScale);
    }
  }
}
