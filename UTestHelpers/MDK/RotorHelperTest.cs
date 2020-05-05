using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using IngameScript.Mockups.Blocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VRageMath;
using TestRunner;

namespace IngameScript.MDK {
  class RotorHelperTest {
    static void assertAnglesClose(float expected, float actual) => Asserts.AreClose(expected, 0.0001f, MathHelper.ToDegrees(actual));

    MockMotorStator getStator(float min = float.MinValue, float max = float.MaxValue) {
      return new MockMotorStator {
        Angle = 0,
        LowerLimitDeg = min,
        UpperLimitDeg = max,
      };
    }

    public void NoLimit() {
      MockMotorStator stator = this.getStator();

      Assert.AreEqual(0, stator.AngleProxy(0));
      assertAnglesClose(150, stator.AngleProxy(MathHelper.ToRadians(150)));
      assertAnglesClose(-150, stator.AngleProxy(MathHelper.ToRadians(-150)));
      assertAnglesClose(-60, stator.AngleProxy(MathHelper.ToRadians(300)));

      stator.Angle = MathHelper.ToRadians(300);

      assertAnglesClose(60, stator.AngleProxy(0));

      stator.Angle = MathHelper.ToRadians(150);

      assertAnglesClose(60, stator.AngleProxy(MathHelper.ToRadians(-150)));

      stator.Angle = MathHelper.ToRadians(-150);

      assertAnglesClose(-60, stator.AngleProxy(MathHelper.ToRadians(150)));
    }

    public void UpperLimit() {
      MockMotorStator stator = this.getStator(max: 330);

      stator.Angle = MathHelper.ToRadians(300);

      assertAnglesClose(-300, stator.AngleProxy(0));
    }

    public void LowerLimit() {
      MockMotorStator stator = this.getStator(min: 30);

      stator.Angle = MathHelper.ToRadians(60);

      assertAnglesClose(300, stator.AngleProxy(0));
    }
  }
}
