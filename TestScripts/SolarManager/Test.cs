using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class TestSuite {
      public TestSuite() {
        Tests.Add(new Marshaller.TestMarshaller());
        Tests.Add(new Unmarshaller.TestUnmarshaller());
        Tests.Add(new DayTracker.TestDayTracker());
        Tests.Add(new TimeStruct.TestTimeStruct());
      }
      public void TestAll() {
        foreach (Test test in Tests) {
          TestDisplay.Write("Testing " + test.GetType().ToString());
          test.DoTests();
          Display(test);
          TestDisplay.Write("");
        }
        TestDisplay.Write("Total: " + TotalNumberTests.ToString() + " tests run, " + TotalNumberFailedTests.ToString() + " failures");
        TestDisplay.Flush();
      }
      private void Display(Test test) {
        TotalNumberTests += test.NumberTests;
        TotalNumberFailedTests += test.NumberFailedTests;
        if (test.NumberFailedTests == 0) {
          TestDisplay.Write("Success!");
        } else {
          TestDisplay.Write("Failure.");
        }
        TestDisplay.Write(test.NumberTests.ToString() + " tests run. " + test.NumberFailedTests.ToString() + " failed");
        foreach (String s in test.Errors) {
          TestDisplay.Write(s);
        }
      }
      abstract public class Test {
        abstract public void DoTests();
        protected void Assert(bool result, String errorMessage) {
          ++NumberTests;
          if (!result) {
            ++NumberFailedTests;
            Errors.Add(errorMessage);
          }
        }
        protected void AssertEqual<T>(T actual, T expected, String message) {
          if (actual == null && expected == null) {
            Assert(true, "");
          } else if (actual == null && expected != null) {
            Assert(false, message + " Expected " + expected.ToString() + ", got null");
          } else if (actual != null && expected == null) {
            Assert(false, message + " Expected null, got " + actual.ToString());
          } else {
            Assert(actual.Equals(expected), message + " Expected " + expected.ToString() + ", got " + actual.ToString());
          }
        }
        public int NumberTests = 0;
        public int NumberFailedTests = 0;
        public List<String> Errors = new List<String>();
      }
      public static TestSuite AllTests = new TestSuite();
      SolarDisplay TestDisplay = new SolarDisplay("Solar Test LCD Panel", false);
      int TotalNumberTests = 0;
      int TotalNumberFailedTests = 0;
      List<Test> Tests = new List<Test>();
    }
  }
}
