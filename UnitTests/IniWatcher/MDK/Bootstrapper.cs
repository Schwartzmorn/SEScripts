﻿using Malware.MDKUtilities;

namespace IngameScript.MDK {
  public class TestBootstrapper {
    static TestBootstrapper() {
      MDKUtilityFramework.Load();
    }

    public static int Main() {
      var runner = new TestRunner.TestRunner();
      runner.AddTest(new IniWatcherTest());
      runner.AddTest(new IniHelperTest());
      return runner.RunTests();
    }
  }
}