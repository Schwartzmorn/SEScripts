namespace IniWatcherTest;

using System;
using System.Linq;
using FakeItEasy;
using IngameScript;
using NUnit.Framework;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

[TestFixture]
class IniWatcherTest
{

  Program.IProcessManager _mockSpawner;
  IMyDoor _mockBlock;

  [SetUp]
  public void SetUp()
  {
    _mockSpawner = Program.Process.CreateManager();
    _mockBlock = A.Fake<IMyDoor>();
  }

  private void _tick()
  {
    foreach (var i in Enumerable.Range(0, 100))
    {
      _mockSpawner.Tick();
    }
  }

  [Test]
  public void It_Parses_Automatically()
  {
    A.CallTo(() => _mockBlock.CustomData).Returns(@"[test-section]
test-key=test-value");
    var ini = new Program.IniWatcher(_mockBlock, _mockSpawner);

    Assert.That(ini.Get("test-section", "test-key").ToString(), Is.EqualTo("test-value"), "The parsing is done at construction");
  }

  [Test]
  public void It_Throws_Errors_On_Parse_Error()
  {
    A.CallTo(() => _mockBlock.CustomData).Returns(@"[test-section]
test-key=test-value
[ha");

    Assert.Throws<InvalidOperationException>(() => new Program.IniWatcher(_mockBlock, _mockSpawner));
  }

  [Test]
  public void It_Feeds_Consumers_Automatically()
  {
    var consumer1 = A.Fake<Program.IIniConsumer>();
    var consumer2 = A.Fake<Program.IIniConsumer>();

    _mockBlock.CustomData = @"[test-section]
test-key=test-value";

    var ini = new Program.IniWatcher(_mockBlock, _mockSpawner);

    ini.Add(consumer1);
    ini.Add(consumer2);

    A.CallTo(() => consumer1.Read(A<MyIni>.Ignored)).MustNotHaveHappened();

    _tick();

    A.CallTo(() => consumer1.Read(A<MyIni>.Ignored)).MustNotHaveHappened();

    _mockBlock.CustomData = @"[test-section]
test-key=test-value2";
    _tick();

    A.CallTo(() => consumer1.Read(A<MyIni>.Ignored)).MustHaveHappenedOnceExactly();
    A.CallTo(() => consumer2.Read(A<MyIni>.Ignored)).MustHaveHappenedOnceExactly();
  }
}
