namespace AutopilotTest;

using System.Collections.Generic;
using IngameScript;
using NUnit.Framework;
using Utilities.Mocks;
using Utilities.Mocks.Base;
using VRageMath;

[TestFixture]
public class WPNetworkTest
{
  MyRemoteControlMock _remote;
  Program.ISaveManager _spawner;

  private Program.WPNetwork _getNetwork() => new(_remote, s => System.Diagnostics.Debug.WriteLine(s), _spawner);

  [SetUp]
  public void SetUp()
  {
    _remote = new MyRemoteControlMock(new MyCubeGridMock(new MyGridTerminalSystemMock(new Utilities.TestBed())));

    _spawner = Program.Process.CreateManager();
  }

  [Test]
  public void It_Parses_CustomData()
  {
    _remote.CustomData = @"[gps]
  WP Z=0:0:1::
  WP A=0:2.5:0:Good:WP B,WP Z
  WP B=1:0:0:Normal:WP A,WP C
";
    Program.WPNetwork network = _getNetwork();

    Assert.That(network.GetWaypoint("WP C"), Is.Null);

    Program.APWaypoint wpz = network.GetWaypoint("WP Z");
    Program.APWaypoint wpa = network.GetWaypoint("WP A");
    Program.APWaypoint wpb = network.GetWaypoint("WP B");

    Assert.That(wpz.Terrain, Is.EqualTo(Program.Terrain.Normal));
    Assert.That(wpz.Name, Is.EqualTo("WP Z"));
    Assert.That(wpz.LinkedWps.Count, Is.EqualTo(0));
    Assert.That(wpz.Coords, Is.EqualTo(new Vector3D(0, 0, 1)));

    Assert.That(wpa.Terrain, Is.EqualTo(Program.Terrain.Good));
    Assert.That(wpa.Name, Is.EqualTo("WP A"));
    Assert.That(wpa.LinkedWps.Count, Is.EqualTo(2));
    Assert.That(wpa.LinkedWps.Contains(wpz));
    Assert.That(wpa.LinkedWps.Contains(wpb));
    Assert.That(wpa.Coords, Is.EqualTo(new Vector3D(0, 2.5, 0)));

    Assert.That(wpb.Name, Is.EqualTo("WP B"));
    Assert.That(wpb.LinkedWps.Count, Is.EqualTo(1));
    Assert.That(wpb.Coords, Is.EqualTo(new Vector3D(1, 0, 0)));
    Assert.That(wpb.LinkedWps.Contains(wpa));
  }

  [Test]
  public void It_Allows_Adding_Waypoints()
  {
    _remote.Waypoints = [
          new("New", new Vector3D(1, 1, 1)),
          new("Updated", new Vector3D(2, 2, 2))
        ];
    _remote.CustomData = @"[gps]
  Updated=0:0:0::Untouched
  Untouched=3:3:3::Updated";

    Program.WPNetwork network = _getNetwork();

    Program.APWaypoint wpNew = network.GetWaypoint("New");
    Program.APWaypoint wpUpdated = network.GetWaypoint("Updated");
    Program.APWaypoint wpUntouched = network.GetWaypoint("Untouched");

    Assert.That(wpNew.Name, Is.EqualTo("New"));
    Assert.That(wpNew.Coords, Is.EqualTo(new Vector3D(1, 1, 1)));

    Assert.That(wpUpdated.Name, Is.EqualTo("Updated"));
    Assert.That(wpUpdated.Coords, Is.EqualTo(new Vector3D(2, 2, 2)));
    Assert.That(wpUntouched, Is.SameAs(wpUpdated.LinkedWps[0]));

    Assert.That(wpUntouched.Name, Is.EqualTo("Untouched"));
    Assert.That(wpUntouched.Coords, Is.EqualTo(new Vector3D(3, 3, 3)));
    Assert.That(wpUpdated, Is.SameAs(wpUntouched.LinkedWps[0]));
  }

  [Test]
  public void It_Allows_Adding_Linked_Waypoints()
  {
    _remote.CustomData = @"[gps]
  WP A=0:0:0::WP B
  WP B=1:1:1::WP A";

    _remote.WorldPositionMock = new Vector3D(2, 2, 2);

    Program.WPNetwork network = _getNetwork();

    network.AddLinkedWP("WP C", "WP B");

    Program.APWaypoint wpA = network.GetWaypoint("WP A");
    Program.APWaypoint wpB = network.GetWaypoint("WP B");
    Program.APWaypoint wpC = network.GetWaypoint("WP C");

    Assert.That(wpC.Name, Is.EqualTo("WP C"));
    Assert.That(wpC.Coords, Is.EqualTo(new Vector3D(2, 2, 2)));
    Assert.That(wpB, Is.SameAs(wpC.LinkedWps[0]));

    Assert.That(wpB.LinkedWps.Count, Is.EqualTo(2));
    Assert.That(wpB.LinkedWps.Contains(wpA));
    Assert.That(wpB.LinkedWps.Contains(wpC));
  }

  [Test]
  public void GetPath_Computes_A_Route()
  {
    _remote.CustomData = @"[gps]
A=0:0:0::B
B=1:0:0::A,C,D
C=2:0:0::B,D
D=2:0:1::B,C";

    Program.WPNetwork network = _getNetwork();

    Program.APWaypoint wpA = network.GetWaypoint("A");
    Program.APWaypoint wpB = network.GetWaypoint("B");
    Program.APWaypoint wpC = network.GetWaypoint("C");
    Program.APWaypoint wpD = network.GetWaypoint("D");

    var wps = new List<Program.APWaypoint>();

    network.GetPath(new Vector3D(-1, 0, -1), wpD, wps);

    Assert.That(wps.Count, Is.EqualTo(3));
    Assert.That(wpA, Is.SameAs(wps[2]));
    Assert.That(wpB, Is.SameAs(wps[1]));
    Assert.That(wpD, Is.SameAs(wps[0]));

    network.GetPath(new Vector3D(0.5, 0, -1), wpD, wps);

    Assert.That(wps.Count, Is.EqualTo(3));
    Assert.That(wps[2].Name.StartsWith(','));
    Assert.That(wps[2].Coords, Is.EqualTo(new Vector3D(0.75, 0, 0)));
    Assert.That(wpB, Is.SameAs(wps[1]));
    Assert.That(wpD, Is.SameAs(wps[0]));
  }
}
