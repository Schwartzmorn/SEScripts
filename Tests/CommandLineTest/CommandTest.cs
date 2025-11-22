
namespace CommandLineTest;

using IngameScript;
using NUnit.Framework;

[TestFixture]
class CommandTest
{

  private Program.IProcessManager _manager;

  [SetUp]
  public void SetUp()
  {
    _manager = Program.Process.CreateManager();
  }

  private void _log(string s) { }

  [Test]
  public void It_Checks_The_Input_Number()
  {
    var mock = new CommandLineTest.MockCommand();

    var command = new Program.Command("default", mock.Provider, "");
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(["a", "b", "c"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);

    command = new Program.Command("fixed number of args", mock.Provider, "", nArgs: 3);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
    Assert.That(command.Spawn(["a", "b"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
    Assert.That(command.Spawn(["a", "b", "c"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(["a", "b", "c", "d"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Null);

    command = new Program.Command("fixed number of args", mock.Provider, "", minArgs: 2, maxArgs: 4);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
    Assert.That(command.Spawn(["a", "b"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(["a", "b", "c"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(["a", "b", "c", "d"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(["a", "b", "c", "d", "e"], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
  }

  [Test]
  public void It_Checks_The_Trigger_For_The_Command()
  {
    var mock = new CommandLineTest.MockCommand();

    var command = new Program.Command("antenna", mock.Provider, "", requiredTrigger: Program.CommandTrigger.Antenna);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Antenna), Is.Not.Null);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.User), Is.Not.Null);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);

    command = new Program.Command("user", mock.Provider, "");
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Antenna), Is.Null);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.User), Is.Not.Null);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);

    command = new Program.Command("cmd", mock.Provider, "", requiredTrigger: Program.CommandTrigger.Cmd);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Antenna), Is.Null);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.User), Is.Null);
    Assert.That(command.Spawn([], _log, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
  }

  [Test]
  public void It_Can_Display_The_Expected_Number_Of_Arguments()
  {
    var mock = new CommandLineTest.MockCommand();
    string log = null;

    var command = new Program.Command("cmd", mock.Provider, "");
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes any number of arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", nArgs: 0);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: (no argument)", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", nArgs: 1);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes 1 argument", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", nArgs: 3);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes 3 arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", minArgs: 2);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes at least 2 arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", maxArgs: 1);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes up to 1 argument", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", maxArgs: 4);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes up to 4 arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", minArgs: 2, maxArgs: 4);
    command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
    Assert.That("-cmd: takes 2-4 arguments", Is.EqualTo(log));
  }
}
