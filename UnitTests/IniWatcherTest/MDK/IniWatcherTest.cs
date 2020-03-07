using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class IniWatcherTest {
    Program.ProcessSpawnerMock mockSpawner;
    Mockups.Blocks.MockDoor mockBlock;
    public void BeforeEach() {
      this.mockSpawner = new Program.ProcessSpawnerMock();
      this.mockBlock = new Mockups.Blocks.MockDoor();
    }

    public void Parse() {
      this.mockBlock.CustomData = @"[test-section]
test-key=test-value";

      var ini = new Program.IniWatcher(this.mockBlock, this.mockSpawner);

      Assert.AreEqual("test-value", ini.Get("test-section", "test-key").ToString(), "The parsing is done at construction");
    }

    public void ParseError() {
      this.mockBlock.CustomData = @"[test-section]
test-key=test-value
[ha";

      try {
        var ini = new Program.IniWatcher(this.mockBlock, this.mockSpawner);
        Assert.Fail("Should have thrown");
      } catch (InvalidOperationException) { }
    }

    public void Consumers() {
      var consumer1 = new Program.ConsumerMock();
      var consumer2 = new Program.ConsumerMock();

      this.mockBlock.CustomData = @"[test-section]
test-key=test-value";

      var ini = new Program.IniWatcher(this.mockBlock, this.mockSpawner);

      ini.Add(consumer1);
      ini.Add(consumer2);

      Assert.IsFalse(consumer1.Called);

      mockSpawner.MockProcessTick();

      Assert.IsFalse(consumer1.Called, "The text has not changed, consumers are not notified");

      this.mockBlock.CustomData = @"[test-section]
test-key=test-value2";
      mockSpawner.MockProcessTick();

      Assert.IsTrue(consumer1.Called);
      Assert.IsTrue(consumer2.Called);
    }
  }
}
