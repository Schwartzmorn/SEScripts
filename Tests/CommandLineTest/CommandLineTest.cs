namespace CommandLineTest;

using System;
using System.Collections.Generic;
using System.Linq;
using IngameScript;
using NUnit.Framework;
using VRage.Game.ModAPI.Ingame.Utilities;

[TestFixture]
class CommandLineTest
{

  private Program.CommandLine _cmdLine;
  private readonly List<string> _logs = new List<string>();
  private Program.IProcessManager _manager;

  public class MockCommand()
  {
    readonly List<List<string>> _calls = [];
    public Action<Program.Process> Provider(Program.ArgumentsWrapper args, Action<string> _logger)
    {
      _calls.Add([.. args]);
      return null;
    }
    public bool Called => _calls.Count > 0;
    public int CallCount => _calls.Count;
    public List<string> GetCall(int i) => _calls[i];
  }

  [SetUp]
  public void SetUp()
  {
    _logs.Clear();
    _manager = Program.Process.CreateManager();
    _cmdLine = new Program.CommandLine("Test", s => _logs.Add(s), _manager);
    _manager.KillAll();
  }

  [Test]
  public void It_Parses_Arguments()
  {
    var mock = new MockCommand();
    _cmdLine.RegisterCommand(new Program.Command("test", mock.Provider, "Brief Help"));

    var p = _cmdLine.StartCmd("test \"-command\"", Program.CommandTrigger.Cmd);

    Assert.That(p, Is.Not.Null);
    Assert.That(mock.Called);
    Assert.That(new List<string>(["-command"]), Is.EqualTo(mock.GetCall(0)));

    p = _cmdLine.StartCmd("test test \"-command\" \"test with spaces\" \"-8\"", Program.CommandTrigger.Cmd);

    Assert.That(p, Is.Not.Null);
    Assert.That(mock.CallCount, Is.EqualTo(2));
    Assert.That(new List<string>(["test", "-command", "test with spaces", "-8"]), Is.EqualTo(mock.GetCall(1)));
  }

  [Test]
  public void It_Allows_Displaying_Help_For_All_Commands()
  {
    var mock = new MockCommand();
    _cmdLine.RegisterCommand(new Program.Command("test1", mock.Provider, "Brief Help test1"));
    _cmdLine.RegisterCommand(new Program.Command("test2", mock.Provider, "Brief Help test2"));
    _cmdLine.RegisterCommand(new Program.Command("test3", mock.Provider, "Brief Help test3", requiredTrigger: Program.CommandTrigger.Cmd));

    var p = _cmdLine.StartCmd("help", Program.CommandTrigger.Cmd);
    _logs.Clear();

    _manager.Tick();

    Assert.That(_logs.Count, Is.EqualTo(6), "One for the introduction + 1 for each command except test3 (help, kill, ps, test1, test2)");
    foreach (string s in new List<string> { "help", "kill", "ps", "test1", "test2" })
    {
      Assert.That(_logs.FirstOrDefault(l => l.StartsWith($"{s}")), Is.Not.Null);
    }
    Assert.That(_logs.FirstOrDefault(l => l == "test1: Brief Help test1"), Is.Not.Null);
    Assert.That(p.Alive, Is.False);
    Assert.That(_logs.Any(l => l.Contains("test3")), Is.False, "Commands that cannot be run by the User are not displayed");
  }

  [Test]
  public void It_Allows_Display_Help_For_A_Single_Command()
  {
    var mock = new MockCommand();
    _cmdLine.RegisterCommand(new Program.Command("testhelp", mock.Provider, "line1\nline2", minArgs: 4));

    _logs.Clear();
    _cmdLine.StartCmd("testhelp -h", Program.CommandTrigger.Cmd);

    Assert.That(_logs.Count, Is.EqualTo(3));
    Assert.That(_logs[0], Is.EqualTo("testhelp: line1"));
    Assert.That(_logs[1], Is.EqualTo("line2"));
    Assert.That(_logs[2], Is.EqualTo("  Takes at least 4 arguments"));
  }

  [Test]
  public void It_Provides_A_Ps_Command()
  {
    var mock = new MockCommand();
    _cmdLine.RegisterCommand(new Program.Command("testps", mock.Provider, ""));
    var p = _cmdLine.StartCmd("testps", Program.CommandTrigger.Cmd);
    var grandChild = p.Spawn(null, "child").Spawn(null, "granchild");
    p.Done();
    _manager.Spawn(null, "Killed").Kill();
    _manager.Spawn(null, "Alive");

    _cmdLine.StartCmd("ps", Program.CommandTrigger.Cmd);

    _logs.Clear();
    _manager.Tick();

    foreach (string s in new List<string> { "testps", "granchild", "Alive" })
    {
      Assert.That(_logs.FirstOrDefault(l => l.Contains(s)), Is.Not.Null);
    }
    Assert.That(_logs.FirstOrDefault(l => l.Contains("Killed")), Is.Null);
  }

  [Test]
  public void It_Provides_A_Kill_Command()
  {
    var p1 = _manager.Spawn(p => { }, "testkill");
    var p2 = _manager.Spawn(p => { }, "testkill");

    _manager.Tick();

    Assert.That(p1.Active);
    Assert.That(p2.Active);

    _cmdLine.StartCmd($"kill {p1.ID}", Program.CommandTrigger.Cmd);
    _manager.Tick();

    Assert.That(p1.Alive, Is.False);
    Assert.That(p2.Active);

    _cmdLine.StartCmd($"kill testkill", Program.CommandTrigger.Cmd);
    _manager.Tick();

    Assert.That(p1.Alive, Is.False);
    Assert.That(p2.Alive, Is.False);
  }
}
