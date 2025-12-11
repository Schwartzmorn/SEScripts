
namespace CommandLineTest;

using System;
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

  private void _spawnAndTick(Program.AbstractCommand cmd, Program.ArgumentsWrapper args, Action<string> logger)
  {
    cmd.Spawn(args, logger, null, _manager, Program.CommandTrigger.Cmd);
    _manager.Tick();
  }

  [Test]
  public void It_Checks_The_Input_Number()
  {
    var mock = new CommandLineTest.MockCommand();

    var command = new Program.Command("default", mock.Provider, "");
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b", "c"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);

    command = new Program.Command("fixed number of args", mock.Provider, "", nArgs: 3);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b", "c"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b", "c", "d"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Null);

    command = new Program.Command("fixed number of args", mock.Provider, "", minArgs: 2, maxArgs: 4);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b", "c"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b", "c", "d"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper(["a", "b", "c", "d", "e"]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Null);
  }

  [Test]
  public void It_Checks_The_Trigger_For_The_Command()
  {
    var mock = new CommandLineTest.MockCommand();

    var command = new Program.Command("antenna", mock.Provider, "", requiredTrigger: Program.CommandTrigger.Antenna);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Antenna), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.User), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);

    command = new Program.Command("user", mock.Provider, "");
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Antenna), Is.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.User), Is.Not.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);

    command = new Program.Command("cmd", mock.Provider, "", requiredTrigger: Program.CommandTrigger.Cmd);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Antenna), Is.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.User), Is.Null);
    Assert.That(command.Spawn(new Program.ArgumentsWrapper([]), null, null, _manager, Program.CommandTrigger.Cmd), Is.Not.Null);
  }

  [Test]
  public void It_Can_Display_The_Expected_Number_Of_Arguments()
  {
    var mock = new CommandLineTest.MockCommand();
    string log = null;
    void logger(string s) { if (!string.IsNullOrWhiteSpace(s)) log = s; }

    var args = new Program.ArgumentsWrapper([], ["h"]);

    var command = new Program.Command("cmd", mock.Provider, "");
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes any number of arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", nArgs: 0);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes no arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", nArgs: 1);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes 1 argument", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", nArgs: 3);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes 3 arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", minArgs: 2);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes at least 2 arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", maxArgs: 1);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes up to 1 argument", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", maxArgs: 4);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes up to 4 arguments", Is.EqualTo(log));

    command = new Program.Command("cmd", mock.Provider, "", minArgs: 2, maxArgs: 4);
    _spawnAndTick(command, args, logger);
    Assert.That("  Takes 2-4 arguments", Is.EqualTo(log));
  }
}
