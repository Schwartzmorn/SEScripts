using Malware.MDKUtilities;

namespace IngameScript.MDK {
  public class TestBootstrapper {
    static TestBootstrapper() {
      MDKUtilityFramework.Load();
    }

    public static int Main() {
      var runner = new TestRunner.TestRunner();
      runner.AddTest(new InstructionsTest());
      runner.AddTest(new RoutineParserTest());
      runner.AddTest(new AutoRoutineHandlerTest());
      return runner.RunTests();
    }
  }
}