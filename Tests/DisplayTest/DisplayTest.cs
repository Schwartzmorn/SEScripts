namespace DisplayTest;

using System;
using IngameScript;
using NUnit.Framework;
using Utilities.Mocks;
using VRage.Game.GUI.TextPanel;
using VRageMath;

[TestFixture]
public class DisplayTest
{
  readonly Program.ColorScheme _scheme = new();
  MyTextSurfaceMock _surface;

  private void _checkVector(Vector2 expected, Vector2 actual)
  {
    Vector2 diff = expected - actual;
    Assert.That(diff.X, Is.EqualTo(0).Within(0.001), $"actual: {actual} expected: {expected}");
    Assert.That(diff.Y, Is.EqualTo(0).Within(0.001), $"actual: {actual} expected: {expected}");
  }

  [SetUp]
  public void SetUp()
  {
    _surface = new MyTextSurfaceMock();
  }

  [Test]
  public void It_Helps_Drawing_Shapes()
  {
    var display = new Program.Display(_surface, offset: new Vector2(10f, 30f), scale: 2);

    var circle = new Program.Shape("Circle", Color.White, new Vector2(50, 50), new Vector2(30, 30), centered: true);
    var square = new Program.Shape("SquareSimple", Color.White, new Vector2(50, 50), new Vector2(30, 30));
    var cross = new Program.Shape("Cross", Color.White, new Vector2(20, 20), new Vector2(30, 10), MathHelper.PiOver2);

    using (Program.Display.Frame frame = display.DrawFrame())
    {
      frame.Draw(circle);
      frame.Draw(square);
      frame.Draw(cross);
    }

    var sprites = _surface.LastDrawnSprites;

    Assert.That(sprites.Count, Is.EqualTo(5), "Background + grid pattern + whatever other sprites were added");

    MySprite sprite = sprites[1];

    Assert.That(sprite.Data, Is.EqualTo("Circle"));
    Assert.That(sprite.Color, Is.EqualTo(Color.White));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(110f, 130f)));
    Assert.That(sprite.Size, Is.EqualTo(new Vector2(60f, 60f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(0));

    sprite = sprites[2];

    Assert.That(sprite.Data, Is.EqualTo("SquareSimple"));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(140f, 160f)));
    Assert.That(sprite.Size, Is.EqualTo(new Vector2(60f, 60f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(0));

    sprite = sprites[3];

    Assert.That(sprite.Data, Is.EqualTo("Cross"));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(80f, 80f)));
    Assert.That(sprite.Size, Is.EqualTo(new Vector2(60f, 20f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(MathHelper.PiOver2));
  }

  [Test]
  public void It_Helps_Drawing_Text()
  {
    var display = new Program.Display(_surface, offset: new Vector2(10f, 30f), scale: 2);

    using (Program.Display.Frame frame = display.DrawFrame())
    {
      frame.DrawText("Some text 1", new Vector2(20, 20), scale: 0.5f);
      frame.DrawText("Some text 2", new Vector2(50, 50), _scheme.MedLight, 2, TextAlignment.LEFT);
    }

    var sprites = _surface.LastDrawnSprites;

    Assert.That(sprites.Count, Is.EqualTo(4), "Background + grid pattern + whatever other sprites were added");

    MySprite sprite = sprites[1];

    Assert.That(sprite.Data, Is.EqualTo("Some text 1"));
    Assert.That(sprite.FontId, Is.EqualTo("Monospace"));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(50f, 70f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(1));
    Assert.That(sprite.Alignment, Is.EqualTo(TextAlignment.CENTER));

    sprite = sprites[2];

    Assert.That(sprite.Data, Is.EqualTo("Some text 2"));
    Assert.That(sprite.FontId, Is.EqualTo("Monospace"));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(110f, 130f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(4));
    Assert.That(sprite.Alignment, Is.EqualTo(TextAlignment.LEFT));
  }

  [Test]
  public void It_Allows_Drawing_ShapeCollections()
  {
    var collection = new Program.ShapeCollections();

    collection.Parse(@"
      =Collection
      Triangle: light: 30,40: 10,10: 0: center
      Circle  : dark : 10,30:  5, 5: 1.570796: center
      ");

    var display = new Program.Display(_surface, offset: new Vector2(10f, 50f), scale: 0.5f, sprites: collection);

    using (Program.Display.Frame frame = display.DrawFrame())
    {
      frame.DrawCollection("Collection");
    }

    var sprites = _surface.LastDrawnSprites;

    Assert.That(sprites.Count, Is.EqualTo(4), "Background + grid pattern + whatever other sprites were added");

    MySprite sprite = sprites[1];

    Assert.That(sprite.Data, Is.EqualTo("Triangle"));
    Assert.That(sprite.Color, Is.EqualTo(_scheme.Light));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(25f, 70f)));
    Assert.That(sprite.Size, Is.EqualTo(new Vector2(5f, 5f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(0));

    sprite = sprites[2];

    Assert.That(sprite.Data, Is.EqualTo("Circle"));
    Assert.That(sprite.Color, Is.EqualTo(_scheme.Dark));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(15f, 65f)));
    Assert.That(sprite.Size, Is.EqualTo(new Vector2(2.5f, 2.5f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(MathHelper.PiOver2));
  }

  [Test]
  public void It_Allows_Drawing_Collections_With_Transformation()
  {
    var collection = new Program.ShapeCollections();

    collection.Parse(@"
      =Collection
      Triangle: light: 30,40: 10,10: 0: center
      Circle  : dark : 10,30:  5, 5: 1.570796326794896619: center
      ");

    var display = new Program.Display(_surface, offset: new Vector2(10f, 50f), scale: 0.5f, sprites: collection);

    using (Program.Display.Frame frame = display.DrawFrame())
    {
      frame.DrawCollection("Collection", new Vector2(10, 20), Convert.ToSingle(Math.PI) / 2, _scheme.MedDark);
    }

    var sprites = _surface.LastDrawnSprites;

    Assert.That(sprites.Count, Is.EqualTo(4), "Background + grid pattern + whatever other sprites were added");

    MySprite sprite = sprites[1];

    Assert.That(sprite.Data, Is.EqualTo("Triangle"));
    Assert.That(sprite.Color, Is.EqualTo(_scheme.MedDark));
    _checkVector(new Vector2(-5, 75), sprite.Position.Value);
    Assert.That(sprite.RotationOrScale, Is.EqualTo(Convert.ToSingle(Math.PI) / 2));

    sprite = sprites[2];

    Assert.That(sprite.Data, Is.EqualTo("Circle"));
    Assert.That(sprite.Color, Is.EqualTo(_scheme.MedDark));
    Assert.That(sprite.Position, Is.EqualTo(new Vector2(0, 65)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(Convert.ToSingle(Math.PI)));
  }
}
