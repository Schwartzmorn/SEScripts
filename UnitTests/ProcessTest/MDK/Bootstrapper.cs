using Malware.MDKUtilities;

namespace IngameScript.MDK {
  public class TestBootstrapper {

    static TestBootstrapper() {
      // Initialize the MDK utility framework
      MDKUtilityFramework.Load();
    }

    public static void Main() {
      var runner = new TestRunner.TestRunner();
      runner.AddTest(new ProcessTest());
      runner.AddTest(new ProcessManagerTest());
      runner.RunTests();
    }
  }
}