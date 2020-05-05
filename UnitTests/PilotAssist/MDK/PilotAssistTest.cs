using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class PilotAssistTest {
    Mockups.Blocks.MockShipController controller1;
    Mockups.Blocks.MockShipController controller2;
    Mockups.MockGridTerminalSystem gts;
    Program.WheelsController mockWheelsController;
    Program.ProcessSpawnerMock spawner;
    public void BeforeEach() {
      this.controller1 = new Mockups.Blocks.MockShipController() {
        CustomName = "Controller 1",
        DisplayNameText = "Controller 1"
      };
      this.controller2 = new Mockups.Blocks.MockShipController() {
        CustomName = "Controller 2",
        DisplayNameText = "Controller 2"
      };
      this.gts = new Mockups.MockGridTerminalSystem() {
        this.controller1,
        this.controller2
      };
      this.mockWheelsController = new Program.WheelsController();
      this.spawner = new Program.ProcessSpawnerMock();
    }

    public void InitWithoutController() {
      this.controller1.CustomData = @"[pilot-assist]
      controllers=Controller 3";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);
      try {
        new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);
        Assert.Fail("Should have raised an error as there is no valid controller");
      } catch {}
    }

    public void InitWithAssistWithoutHandbrake() {
      this.controller1.CustomData = @"[pilot-assist]
      assist=true
      controllers=Controller 1,Controller 2";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);

      Assert.IsTrue(this.controller2.ControlWheels);
      Assert.IsFalse(this.controller1.HandBrake);

      var pa = new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);

      Assert.IsFalse(this.controller1.ControlWheels, "When assist is true, the controllers no longer control the wheels directly");
      Assert.IsFalse(this.controller2.ControlWheels);
      Assert.IsFalse(this.controller1.HandBrake);

      Assert.IsTrue(this.spawner.HasOnSave);
      Assert.IsFalse(pa.ManuallyBraked);
    }

    public void InitWithoutAssistWithHandbrake() {
      this.controller1.CustomData = @"[pilot-assist]
      controllers=Controller 2";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);

      this.controller2.ControlWheels = false;
      this.controller2.HandBrake = true;

      var pa = new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);

      Assert.IsTrue(this.controller2.ControlWheels);
      Assert.IsTrue(this.controller2.HandBrake);
      Assert.IsTrue(pa.ManuallyBraked);
    }

    public void Save() {
      this.controller1.CustomData = @"[pilot-assist]
      assist=true
      controllers=Controller 1,Controller 2,Controller 3
      sensitivity=4";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);

      new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);

      string saved = this.spawner.GetSavedString();

      Assert.AreEqual("[pilot-assist]\nassist=true\ncontrollers=Controller 1,Controller 2\nsensitivity=4", saved.Trim());
    }

    public void DetectHandbrake() {
      this.controller1.CustomData = @"[pilot-assist]
      controllers=Controller 1";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);

      var pa = new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);

      this.spawner.MockProcessTick();

      Assert.IsFalse(pa.ManuallyBraked);

      this.controller1.HandBrake = true;
      this.spawner.MockProcessTick();

      Assert.IsTrue(pa.ManuallyBraked);

      this.controller1.HandBrake = false;
      this.spawner.MockProcessTick();

      Assert.IsFalse(pa.ManuallyBraked);
    }

    public void AutoBrake() {
      this.controller1.CustomData = @"[pilot-assist]
      controllers=Controller 1";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);
      var handbraker = new Program.MockBraker();
      var deactivator = new Program.MockDeactivator();

      var pa = new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);
      pa.AddBraker(handbraker);
      pa.AddDeactivator(deactivator);

      Assert.IsTrue(this.controller1.IsUnderControl);

      this.spawner.MockProcessTick();

      Assert.IsFalse(this.controller1.HandBrake);

      this.controller1.IsUnderControl = false;
      this.spawner.MockProcessTick();

      Assert.IsTrue(this.controller1.HandBrake);

      this.controller1.IsUnderControl = true;
      this.spawner.MockProcessTick();

      Assert.IsFalse(this.controller1.HandBrake);

      handbraker.Handbrake = true;
      this.spawner.MockProcessTick();

      Assert.IsTrue(this.controller1.HandBrake);

      deactivator.Deactivate = true;
      this.spawner.MockProcessTick();

      Assert.IsTrue(this.controller1.HandBrake, "When deactivated, the handbrake is neither engaged nor disengaged automatically");

      handbraker.Handbrake = false;
      deactivator.Deactivate = false;
      this.spawner.MockProcessTick();

      Assert.IsFalse(this.controller1.HandBrake);

      handbraker.Handbrake = true;
      deactivator.Deactivate = true;
      this.controller1.IsUnderControl = false;
      this.spawner.MockProcessTick();

      Assert.IsFalse(this.controller1.HandBrake, "When deactivated, the handbrake is neither engaged nor disengaged automatically");
    }

    public void PilotAssist() {
      this.controller1.CustomData = @"[pilot-assist]
      assist=true
      controllers=Controller 1
      sensitivity=5";
      var ini = new Program.IniWatcher(this.controller1, this.spawner);
      var deactivator = new Program.MockDeactivator();

      var pa = new Program.PilotAssist(this.gts, ini, null, this.spawner, this.mockWheelsController);
      pa.AddDeactivator(deactivator);

      this.controller1.MoveIndicator = new VRageMath.Vector3(0, 0, 2);
      this.spawner.MockProcessTick();

      Assert.AreEqual(-0.4f, this.mockWheelsController.Power);
      Assert.AreEqual(0, this.mockWheelsController.Steer);

      this.controller1.MoveIndicator = new VRageMath.Vector3(4, 0, 0);
      this.spawner.MockProcessTick();

      Assert.AreEqual(0, this.mockWheelsController.Power);
      Assert.AreEqual(0.8f, this.mockWheelsController.Steer);

      deactivator.Deactivate = true;

      this.controller1.MoveIndicator = new VRageMath.Vector3(0, 0, 4);
      this.spawner.MockProcessTick();

      Assert.AreEqual(0, this.mockWheelsController.Power, "When deactivated, the controls are left as is");
      Assert.AreEqual(0.8f, this.mockWheelsController.Steer);
    }
  }
}
