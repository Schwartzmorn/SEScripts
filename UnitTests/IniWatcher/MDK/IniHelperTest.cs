using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class IniHelperTest {
    public void VectorSerialization() {
      var ini = new VRage.Game.ModAPI.Ingame.Utilities.MyIni();

      ini.SetVector("test-section", "test-vector", new VRageMath.Vector3D(0, 2.5, 3));

      Assert.AreEqual("[test-section]\ntest-vector-x=0\ntest-vector-y=2.5\ntest-vector-z=3\n", ini.ToString());
    }
    public void VectorDeserialization() {
      var ini = new VRage.Game.ModAPI.Ingame.Utilities.MyIni();

      ini.TryParse("[test-section]\ntest-vector-x=0\ntest-vector-y=2.5\ntest-vector-z=3\n");

      Assert.AreEqual(ini.GetVector("test-section", "test-vector"), new VRageMath.Vector3D(0, 2.5, 3));
    }
  }
}
