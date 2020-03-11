using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class ShapeCollectionTest {
    readonly Program.ColorScheme scheme = new Program.ColorScheme();

    private void checkError(Action action, string msg) {
      try {
        action();
        Assert.Fail("Expected the action to have thrown");
      } catch(InvalidOperationException e) {
        Assert.AreEqual(msg, e.Message);
      }
    }

    public void Parse() {
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

      Assert.AreEqual(4, collection1.Count);
      Assert.AreEqual(1, collection2.Count);

      var sprite = collection1[0];
      Assert.AreEqual("Triangle", sprite.Data);
      Assert.AreEqual(this.scheme.Dark, sprite.Color);
      Assert.AreEqual(new VRageMath.Vector2(12.5f, 12.5f), sprite.Position);
      Assert.AreEqual(new VRageMath.Vector2(5f, 5f), sprite.Size);
      Assert.AreEqual(0, sprite.RotationOrScale);

      sprite = collection1[1];
      Assert.AreEqual("Circle", sprite.Data);
      Assert.AreEqual(this.scheme.Light, sprite.Color);
      Assert.AreEqual(new VRageMath.Vector2(10f, 10f), sprite.Position);
      Assert.AreEqual(new VRageMath.Vector2(5f, 5f), sprite.Size);
      Assert.AreEqual(0, sprite.RotationOrScale);

      sprite = collection1[2];
      Assert.AreEqual(this.scheme.MedDark, collection1[2].Color);
      Assert.AreEqual(new VRageMath.Vector2(125f, 125f), sprite.Position);
      Assert.AreEqual(1.570796326794896619f, sprite.RotationOrScale);

      sprite = collection1[3];
      Assert.AreEqual(this.scheme.MedLight, collection1[3].Color);
      Assert.AreEqual(new VRageMath.Vector2(100f, 100f), sprite.Position);
      Assert.AreEqual(1.570796326794896619f, sprite.RotationOrScale);

      sprite = collection2[0];
      Assert.AreEqual(new VRageMath.Color(100, 100, 100), sprite.Color);
      Assert.AreEqual(new VRageMath.Vector2(10f, 10f), sprite.Position);
    }

    public void ParseError() {
      var collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("Triangle:dark:10,10:5,5:0"), "Error at line 1: found sprite before collection name");

      collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("=collection\nTriangle: dark:10, 10"), "Error at line 2: not enough or too many values");

      collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("=collection\nTriangle:dark:10,10:5"), "Error at line 2: not enough or too many values for the size");

      collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("=collection\nTriangle:dark:10,a:5,5"), "Error at line 2: could not parse position");

      collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("=collection\nTriangle:dark:10,10:5,5:a"), "Error at line 2: could not parse rotation");

      collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("=collection\nTriangle:10,10:10,10:5,5"), "Error at line 2: not enough or too many values for the color");

      collection = new Program.ShapeCollections();
      this.checkError(() => collection.Parse("=collection\nTriangle:10,10,a:10,10:5,5"), "Error at line 2: could not parse rgb color");
    }
  }
}
