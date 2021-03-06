﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestRunner {

  class Asserts {
    public static void AreClose(float expected, float error, float actual) {
      if (Math.Abs(expected - actual) > error) {
        throw new AssertFailedException($"Expected:<{expected} ± {error}>, Actual:<{actual}>");
      }
    }
  }


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
    
    readonly List<UnitTest> tests = new List<UnitTest>();
    int failedTests = 0;

    public void AddTest(object o) {
      Action beforeEach = null;
      MethodInfo method = o.GetType().GetMethod("BeforeEach", BindingFlags.Public | BindingFlags.Instance);
      if (method != null) {
        beforeEach = () => method.Invoke(o, null);
      }

      foreach(MethodInfo m in o.GetType().GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)) {
        if (m.Name != "BeforeEach") {
          this.tests.Add(new UnitTest($"{o.GetType().Name}.{m.Name}", () => m.Invoke(o, null), beforeEach));
        }
      }
    }

    public int RunTests() {
      foreach(UnitTest test in this.tests) {
        try {
          Debug.WriteLine($"====== Running {test.Name}");
          test.BeforeEach?.Invoke();
          test.Test();
        } catch (TargetInvocationException e) {
          ++this.failedTests;
          Debug.WriteLine($"====== Test {test.Name} failed");
          Debug.WriteLine(e.InnerException.ToString());
        } catch (Exception e) {
           ++this.failedTests;
          Debug.WriteLine(e.ToString());
        }
      }
      Debug.WriteLine($"{this.tests.Count} test(s) run. {this.failedTests} failed.");
      return this.failedTests;
    }
  }
}
