using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class ConversionHelperTest {
    public void GetAmount() {
      Assert.AreEqual(100, "100 W".GetAmount());
      Assert.AreEqual(1000, "1KW".GetAmount());
      Assert.AreEqual(1000000, "1,000 KW".GetAmount());
      Assert.AreEqual(1500000, "1.5 MW".GetAmount());
    }
    public void FormatAmount() {
      Assert.AreEqual("100 W", 100f.FormatAmount("W"));
      Assert.AreEqual("1.5 KW", 1500f.FormatAmount("W"));
      Assert.AreEqual("3 MWh", 3000000f.FormatAmount("Wh"));
      Assert.AreEqual("3.12 MWh", 3123456f.FormatAmount("Wh"));
    }
  }
}
