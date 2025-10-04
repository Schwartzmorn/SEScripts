namespace IniWatcherTest;

using IngameScript;
using NUnit.Framework;

[TestFixture]
class IniHelperTest
{
  [Test]
  public void It_Helps_With_Vector_Serialization()
  {
    var ini = new VRage.Game.ModAPI.Ingame.Utilities.MyIni();

    ini.SetVector("test-section", "test-vector", new VRageMath.Vector3D(0, 2.5, 3));

    Assert.That(ini.ToString(), Is.EqualTo("[test-section]\ntest-vector-x=0\ntest-vector-y=2.5\ntest-vector-z=3\n"));
  }

  [Test]
  public void It_Helps_With_Vector_Deserialization()
  {
    var ini = new VRage.Game.ModAPI.Ingame.Utilities.MyIni();

    ini.TryParse("[test-section]\ntest-vector-x=0\ntest-vector-y=2.5\ntest-vector-z=3\n");

    Assert.That(new VRageMath.Vector3D(0, 2.5, 3), Is.EqualTo(ini.GetVector("test-section", "test-vector")));
  }
}
