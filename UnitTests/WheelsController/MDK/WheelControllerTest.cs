using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using IngameScript.Mockups;
using IngameScript.Mockups.Blocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class WheelControllerTest {
    public void Everything() {
      var grid = new MockCubeGrid {
        GridSizeEnum = VRage.Game.MyCubeSize.Small
      };

      var controller = new MockShipController {
        CubeGrid = grid,
        ShipMass = new Sandbox.ModAPI.Ingame.MyShipMass(1820, 1820, 1820),
        WorldPosition = Vector3D.Zero,
        WorldMatrix = MatrixD.Identity
      };


      var gts = new MockGridTerminalSystem {
        PowerWheelTest.GetSuspension(new Vector3D(-1, 0, -1), true, grid),
        PowerWheelTest.GetSuspension(new Vector3D(1, 0, -1), false, grid),
        PowerWheelTest.GetSuspension(new Vector3D(-1, 0, 1), true, grid),
        PowerWheelTest.GetSuspension(new Vector3D(1, 0, 1), false, grid),
      };

      var transformer = new Program.CoordinatesTransformer(controller);

      var ini = new VRage.Game.ModAPI.Ingame.Utilities.MyIni();
      ini.TryParse(@"");

      var saveManager = new Program.ProcessSpawnerMock();

      var command = new Program.CommandLine("mock", null, saveManager);

      var wc = new Program.WheelsController(command, controller, gts, ini, saveManager, transformer);

      Assert.AreEqual(new Vector3D(0, 1, 0), wc.GetContactPlaneW());

      Assert.AreEqual(new Vector3D(0, -1.75, -1), wc.GetPointOfContactW(new Vector3D(0, 0, -1)));

      wc.SetPosition("normal"); // too anoying to test
    }
  }
}
