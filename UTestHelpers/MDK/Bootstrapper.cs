using Malware.MDKUtilities;

namespace IngameScript.MDK {
  public class TestBootstrapper {

    static TestBootstrapper() {
      MDKUtilityFramework.Load();
    }

    public static int Main() {
      var runner = new TestRunner.TestRunner();
      runner.AddTest(new DetailedInfoHelperTest());
      runner.AddTest(new ConversionHelperTest());
      runner.AddTest(new RotorHelperTest());
      return runner.RunTests();
    }
  }
}