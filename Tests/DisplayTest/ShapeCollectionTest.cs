namespace DisplayTest;

using System;
using IngameScript;
using NUnit.Framework;

[TestFixture]
public class ShapeCollectionTest
{
  readonly Program.ColorScheme _scheme = new();

  private static void _checkError(Action action, string msg)
  {
    var exception = Assert.Throws<InvalidOperationException>(() => action());
    Assert.That(exception.Message, Is.EqualTo(msg));
  }

  [Test]
  public void It_Parses_Collections()
  {
    var collection = new Program.ShapeCollections();

    collection.Parse(@"
      ;ignored comment
      =    Collection 1
      ; sprite 0 with a scheme color
      Triangle:dark:10,10:5,5:0
      ; sprite 1 also with a scheme color and some spaces thrown in
      Circle : Light : 10 , 10 : 5 , 5 : 0 : center
      ; sprite 2 also with a scheme color, rotated by Pi/2
      Triangle:medDark:100,100:50,50:1.570796326794896619
      ; sprite 3 also with a scheme color, rotated by Pi/2
      Triangle:medlight:100,100:50,50:1.570796326794896619:center
      =    Collection 2  
      ;sprite 0 with a hardcoded color
      Triangle:100,100,100:0,0:-20,-20:50
      ");

    var collection1 = collection.Get("Collection 1");
    var collection2 = collection.Get("Collection 2");

    Assert.That(collection1.Count, Is.EqualTo(4));
    Assert.That(collection2.Count, Is.EqualTo(1));

    var sprite = collection1[0];
    Assert.That(sprite.Data, Is.EqualTo("Triangle"));
    Assert.That(sprite.Color, Is.EqualTo(this._scheme.Dark));
    Assert.That(sprite.Position, Is.EqualTo(new VRageMath.Vector2(12.5f, 12.5f)));
    Assert.That(sprite.Size, Is.EqualTo(new VRageMath.Vector2(5f, 5f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(0));

    sprite = collection1[1];
    Assert.That(sprite.Data, Is.EqualTo("Circle"));
    Assert.That(sprite.Color, Is.EqualTo(this._scheme.Light));
    Assert.That(sprite.Position, Is.EqualTo(new VRageMath.Vector2(10f, 10f)));
    Assert.That(sprite.Size, Is.EqualTo(new VRageMath.Vector2(5f, 5f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(0));

    sprite = collection1[2];
    Assert.That(collection1[2].Color, Is.EqualTo(this._scheme.MedDark));
    Assert.That(sprite.Position, Is.EqualTo(new VRageMath.Vector2(125f, 125f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(1.570796326794896619f));

    sprite = collection1[3];
    Assert.That(collection1[3].Color, Is.EqualTo(this._scheme.MedLight));
    Assert.That(sprite.Position, Is.EqualTo(new VRageMath.Vector2(100f, 100f)));
    Assert.That(sprite.RotationOrScale, Is.EqualTo(1.570796326794896619f));

    sprite = collection2[0];
    Assert.That(sprite.Color, Is.EqualTo(new VRageMath.Color(100, 100, 100)));
    Assert.That(sprite.Position, Is.EqualTo(new VRageMath.Vector2(10f, 10f)));
  }

  [Test]
  public void It_Returns_Meaningful_Parse_Error()
  {
    var collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("Triangle:dark:10,10:5,5:0"), "Error at line 1: found sprite before collection name");

    collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("=collection\nTriangle: dark:10, 10"), "Error at line 2: not enough or too many values");

    collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("=collection\nTriangle:dark:10,10:5"), "Error at line 2: not enough or too many values for the size");

    collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("=collection\nTriangle:dark:10,a:5,5"), "Error at line 2: could not parse position");

    collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("=collection\nTriangle:dark:10,10:5,5:a"), "Error at line 2: could not parse rotation");

    collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("=collection\nTriangle:10,10:10,10:5,5"), "Error at line 2: not enough or too many values for the color");

    collection = new Program.ShapeCollections();
    _checkError(() => collection.Parse("=collection\nTriangle:10,10,a:10,10:5,5"), "Error at line 2: could not parse rgb color");
  }
}
