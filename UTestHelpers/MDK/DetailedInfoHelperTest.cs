using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameScript.Mockups.Blocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class DetailedInfoHelperTest {
    public void GetDetailedInfo() {
      var cube = new MockSolarPanel {
        CustomInfo = @"Max Possible Output: 160 kW
––•",
        DetailedInfo = @"Type: Solar Panel
Max Output: 40.80 kW
Current Output: 0 W
"
      };
      Assert.AreEqual("40.80 kW", cube.GetDetailedInfo("Max Output"));
      Assert.AreEqual("160 kW", cube.GetDetailedInfo("Max Possible Output"));

    }
  }
}
