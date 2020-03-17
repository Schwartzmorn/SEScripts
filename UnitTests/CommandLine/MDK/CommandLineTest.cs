using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class CommandLineTest {

    private Program.CommandLine cmdLine;
    private readonly List<string> logs = new List<string>();
    private Program.IProcessManager manager;

    public void BeforeEach() {
      this.logs.Clear();
      this.manager = Program.Process.CreateManager(null);
      this.cmdLine = new Program.CommandLine("Test", s => this.logs.Add(s), this.manager);
      this.manager.KillAll();
    }

    public class MockCommand {
      List<List<string>> calls = new List<List<string>>();
      readonly int period;
      public MockCommand(int period = 1) {
        this.period = period;
      }
      public VRage.MyTuple<int, bool, Action<Program.Process>> Provider(List<string> args, Action<string> logger) {
        this.calls.Add(args);
        return VRage.MyTuple.Create<int, bool, Action<Program.Process>>(this.period, true, null);
      }
      public bool Called => this.calls.Count > 0;
      public int CallCount => this.calls.Count;
      public List<string> GetCall(int i) => this.calls[i];
    }

    public void BasicParse() {
      var mock = new MockCommand();
      this.cmdLine.RegisterCommand(new Program.Command("test", mock.Provider, "Brief Help"));

      var p = this.cmdLine.StartCmd("-test -command", Program.CommandTrigger.Cmd);

      Assert.IsNull(p, "Cannot parse as there are two commands");
      Assert.IsFalse(mock.Called);

      p = this.cmdLine.StartCmd("-test \"-command\"", Program.CommandTrigger.Cmd);

      Assert.IsNotNull(p, "Can parse as there is only one commands");
      Assert.IsTrue(mock.Called);
      Assert.IsTrue(Enumerable.SequenceEqual(new List<string>{ "-command" }, mock.GetCall(0)));

      p = this.cmdLine.StartCmd("-test test \"-command\" \"test with spaces\" \"-8\"", Program.CommandTrigger.Cmd);

      Assert.IsNotNull(p);
      Assert.AreEqual(2, mock.CallCount);
      Assert.IsTrue(Enumerable.SequenceEqual(new List<string> { "test", "-command", "test with spaces", "-8" }, mock.GetCall(1)));
    }

    public void Help() {
      var mock = new MockCommand();
      this.cmdLine.RegisterCommand(new Program.Command("test1", mock.Provider, "Brief Help test1"));
      this.cmdLine.RegisterCommand(new Program.Command("test2", mock.Provider, "Brief Help test2"));
      this.cmdLine.RegisterCommand(new Program.Command("test3", mock.Provider, "Brief Help test3", requiredTrigger: Program.CommandTrigger.Cmd));

      var p = this.cmdLine.StartCmd("-help", Program.CommandTrigger.Cmd);
      this.logs.Clear();

      this.manager.Tick();

      Assert.AreEqual(6, this.logs.Count, "One for the introduction + 1 for each command except test3 (help, kill, ps, test1, test2)");
      foreach(string s in new List<string>{ "help", "kill", "ps", "test1", "test2" }) {
        Assert.IsNotNull(this.logs.FirstOrDefault(l => l.StartsWith($"-{s}")));
      }
      Assert.IsNotNull(this.logs.FirstOrDefault(l => l == "-test1: Brief Help test1"));
      Assert.IsFalse(p.Alive);
      Assert.IsFalse(this.logs.Any(l => l.Contains("test3")), "Commands that cannot be run by the User are not displayed");
    }

    public void HelpCommand() {
      var mock = new MockCommand();
      this.cmdLine.RegisterCommand(new Program.Command("testhelp", mock.Provider, "", detailedHelp: "line1\nline2", minArgs: 4));

      this.cmdLine.StartCmd("-help testhelp", Program.CommandTrigger.Cmd);
      this.logs.Clear();

      this.manager.Tick();

      Assert.AreEqual(3, this.logs.Count);
      Assert.AreEqual("-testhelp: takes at least 4 arguments", this.logs[0]);
      Assert.AreEqual("  line1", this.logs[1]);
      Assert.AreEqual("  line2", this.logs[2]);
    }

    public void PS() {
      var mock = new MockCommand();
      this.cmdLine.RegisterCommand(new Program.Command("testps", mock.Provider, ""));
      var p = this.cmdLine.StartCmd("-testps", Program.CommandTrigger.Cmd);
      var grandChild = p.Spawn(null, "child").Spawn(null, "granchild");
      p.Done();
      this.manager.Spawn(null, "Killed").Kill();
      this.manager.Spawn(null, "Alive");

      this.cmdLine.StartCmd("-ps", Program.CommandTrigger.Cmd);

      this.logs.Clear();
      this.manager.Tick();

      foreach (string s in new List<string> { "testps", "granchild", "Alive" }) {
        Assert.IsNotNull(this.logs.FirstOrDefault(l => l.Contains(s)));
      }
      Assert.IsNull(this.logs.FirstOrDefault(l => l.Contains("Killed")));
    }

    public void Kill() {
      var mock = new MockCommand(20);
      this.cmdLine.RegisterCommand(new Program.Command("testkill", mock.Provider, ""));
      var p1 = this.cmdLine.StartCmd("-testkill", Program.CommandTrigger.Cmd);
      var p2 = this.cmdLine.StartCmd("-testkill", Program.CommandTrigger.Cmd);

      this.manager.Tick();

      Assert.IsTrue(p1.Active);
      Assert.IsTrue(p2.Active);

      this.cmdLine.StartCmd($"-kill {p1.ID}", Program.CommandTrigger.Cmd);
      this.manager.Tick();

      Assert.IsFalse(p1.Alive);
      Assert.IsTrue(p2.Active);

      this.cmdLine.StartCmd($"-kill testkill", Program.CommandTrigger.Cmd);
      this.manager.Tick();

      Assert.IsFalse(p1.Alive);
      Assert.IsFalse(p2.Alive);
    }
  }
}
