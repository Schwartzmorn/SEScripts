using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestRunner {
  class TestRunner {

    class UnitTest {
      public readonly Action BeforeEach;
      public readonly string Name;
      public readonly Action Test;
      public UnitTest(string name, Action test, Action before) {
        this.Name = name;
        this.Test = test;
        this.BeforeEach = before;
      }
    }
    
    private readonly List<UnitTest> tests = new List<UnitTest>();
    private int failedTests = 0;

    public void AddTest(object o) {
      Action beforeEach = null;
      var method = o.GetType().GetMethod("BeforeEach", BindingFlags.Public | BindingFlags.Instance);
      if (method != null) {
        beforeEach = () => method.Invoke(o, null);
      }

      foreach(var m in o.GetType().GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)) {
        if (m.Name != "BeforeEach") {
          this.tests.Add(new UnitTest($"{o.GetType().Name}.{m.Name}", () => m.Invoke(o, null), beforeEach));
        }
      }
    }

    public void RunTests() {
      foreach(UnitTest test in this.tests) {
        try {
          Debug.WriteLine($"Running {test.Name}");
          test.BeforeEach?.Invoke();
          test.Test();
        } catch (TargetInvocationException e) {
          ++this.failedTests;
          Debug.WriteLine(e.InnerException.ToString());
        } catch (Exception e) {
           ++this.failedTests;
          Debug.WriteLine(e.ToString());
        }
      }
      Debug.WriteLine($"{this.tests.Count} test(s) run. {this.failedTests} failed.");
    }
  }
}
