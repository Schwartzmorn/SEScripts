using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameScript.Mockups.Blocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VRageMath;
using Sandbox.ModAPI.Ingame;

namespace IngameScript.MDK {
  class WPNetworkTest {
    MockRemoteControl remote;
    Program.ProcessSpawnerMock spawner;
    public void BeforeEach() {
      this.remote = new MockRemoteControl() {
        CustomData = ""
      };
      this.spawner = new Program.ProcessSpawnerMock();
    }

    Program.WPNetwork getNetwork() => new Program.WPNetwork(this.remote, s => System.Diagnostics.Debug.WriteLine(s), this.spawner);
    public void Parsing() {
      this.remote.CustomData = @"[WP Z]
  gps=GPS:WP Z:0:0:1:
[WP A]
  gps=GPS:WP A:0:2.5:0:
  type=PrecisePath
  terrain=Good
  linked-wp=WP B,WP Z
[WP B]
  gps=GPS:WP B:1:0:0:
  linked-wp=WP A,WP C
";
      Program.WPNetwork network = this.getNetwork();

      Assert.IsNull(network.GetWaypoint("WP C"));

      Program.APWaypoint wpz = network.GetWaypoint("WP Z");
      Program.APWaypoint wpa = network.GetWaypoint("WP A");
      Program.APWaypoint wpb = network.GetWaypoint("WP B");

      Assert.AreEqual(Program.Terrain.Normal, wpz.Terrain);
      Assert.AreEqual(Program.WPType.Path, wpz.Type);
      Assert.AreEqual("WP Z", wpz.Name);
      Assert.AreEqual(0, wpz.LinkedWps.Count);
      Assert.AreEqual(new Vector3D(0, 0, 1), wpz.Coords);

      Assert.AreEqual(Program.Terrain.Good, wpa.Terrain);
      Assert.AreEqual(Program.WPType.PrecisePath, wpa.Type);
      Assert.AreEqual("WP A", wpa.Name);
      Assert.AreEqual(2, wpa.LinkedWps.Count);
      Assert.IsTrue(wpa.LinkedWps.Contains(wpz));
      Assert.IsTrue(wpa.LinkedWps.Contains(wpb));
      Assert.AreEqual(new Vector3D(0, 2.5, 0), wpa.Coords);

      Assert.AreEqual("WP B", wpb.Name);
      Assert.AreEqual(1, wpb.LinkedWps.Count);
      Assert.AreEqual(new Vector3D(1, 0, 0), wpb.Coords);
      Assert.IsTrue(wpb.LinkedWps.Contains(wpa));
    }

    public void AddWaypoints() {
      this.remote.Waypoints = new List<MyWaypointInfo>() {
        new MyWaypointInfo("New", new Vector3D(1, 1, 1)),
        new MyWaypointInfo("Updated", new Vector3D(2, 2, 2))
      };
      this.remote.CustomData = @"[Updated]
  gps=GPS:Update:0:0:0:
  linked-wp=Untouched
[Untouched]
  gps=GPS:Untouched:3:3:3:
  linked-wp=Updated";

      Program.WPNetwork network = this.getNetwork();

      Program.APWaypoint wpNew = network.GetWaypoint("New");
      Program.APWaypoint wpUpdated = network.GetWaypoint("Updated");
      Program.APWaypoint wpUntouched = network.GetWaypoint("Untouched");

      Assert.AreEqual("New", wpNew.Name);
      Assert.AreEqual(new Vector3D(1, 1, 1), wpNew.Coords);

      Assert.AreEqual("Updated", wpUpdated.Name);
      Assert.AreEqual(new Vector3D(2, 2, 2), wpUpdated.Coords);
      Assert.AreSame(wpUntouched, wpUpdated.LinkedWps[0]);

      Assert.AreEqual("Untouched", wpUntouched.Name);
      Assert.AreEqual(new Vector3D(3, 3, 3), wpUntouched.Coords);
      Assert.AreSame(wpUpdated, wpUntouched.LinkedWps[0]);
    }

    public void AddLinkedWaypoint() {
      this.remote.CustomData = @"[WP A]
  gps=GPS:WP A:0:0:0:
  linked-wp=WP B
[WP B]
  gps=GPS:WP B:1:1:1:
  linked-wp=WP A";

      this.remote.WorldPosition = new Vector3D(2, 2, 2);

      Program.WPNetwork network = this.getNetwork();

      network.AddLinkedWP("WP C", "WP B");

      Program.APWaypoint wpA = network.GetWaypoint("WP A");
      Program.APWaypoint wpB = network.GetWaypoint("WP B");
      Program.APWaypoint wpC = network.GetWaypoint("WP C");

      Assert.AreEqual("WP C", wpC.Name);
      Assert.AreEqual(new Vector3D(2, 2, 2), wpC.Coords);
      Assert.AreSame(wpB, wpC.LinkedWps[0]);

      Assert.AreEqual(2, wpB.LinkedWps.Count);
      Assert.IsTrue(wpB.LinkedWps.Contains(wpA));
      Assert.IsTrue(wpB.LinkedWps.Contains(wpC));
    }

    public void GetPath() {
      this.remote.CustomData = @"[A]
  gps=GPS:A:0:0:0:
  linked-wp=B
[B]
  gps=GPS:B:1:0:0:
  linked-wp=A,C,D
[C]
  gps=GPS:C:2:0:0:
  linked-wp=B,D
[D]
  gps=GPS:D:2:0:1:
  linked-wp=B,C";

      Program.WPNetwork network = this.getNetwork();

      Program.APWaypoint wpA = network.GetWaypoint("A");
      Program.APWaypoint wpB = network.GetWaypoint("B");
      Program.APWaypoint wpC = network.GetWaypoint("C");
      Program.APWaypoint wpD = network.GetWaypoint("D");

      var wps = new List<Program.APWaypoint>();

      network.GetPath(new Vector3D(-1, 0, -1), wpD, wps);

      Assert.AreEqual(3, wps.Count);
      Assert.AreSame(wpA, wps[2]);
      Assert.AreSame(wpB, wps[1]);
      Assert.AreSame(wpD, wps[0]);

      network.GetPath(new Vector3D(0.5, 0, -1), wpD, wps);

      Assert.AreEqual(3, wps.Count);
      Assert.IsTrue(wps[2].Name.StartsWith(","));
      Assert.AreEqual(new Vector3D(0.75, 0, 0), wps[2].Coords);
      Assert.AreSame(wpB, wps[1]);
      Assert.AreSame(wpD, wps[0]);
    }
  }
}
