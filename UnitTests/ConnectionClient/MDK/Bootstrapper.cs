using Malware.MDKUtilities;

namespace IngameScript.MDK {
  public class TestBootstrapper {
    static TestBootstrapper() {
      MDKUtilityFramework.Load();
    }

    public static void Main() {
      var runner = new TestRunner.TestRunner();
      runner.AddTest(new ConnectionClientTest());
      runner.RunTests();
    }
  }
}