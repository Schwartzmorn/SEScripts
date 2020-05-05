using Malware.MDKUtilities;

namespace IngameScript.MDK {
  public class TestBootstrapper {
    static TestBootstrapper() {
      // Initialize the MDK utility framework
      MDKUtilityFramework.Load();
    }

    public static int Main() {
      var runner = new TestRunner.TestRunner();
      runner.AddTest(new SolarManagerTest());
      runner.AddTest(new SolarRotorTest());
      return runner.RunTests();
    }
  }
}