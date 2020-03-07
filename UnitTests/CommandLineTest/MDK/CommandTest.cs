using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class CommandTest {

    private Program.IProcessManager manager;

    public void BeforeEach() => this.manager = Program.Process.CreateManager(null);

    private void log(string s) { }

    public void CheckInput() {
      var mock = new CommandLineTest.MockCommand();

      var command = new Program.Command("default", mock.Provider, "");
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNotNull(command.Spawn(new List<string>{ "a", "b", "c" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));

      command = new Program.Command("fixed number of args", mock.Provider, "", nArgs: 3);
      Assert.IsNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNull(command.Spawn(new List<string>{ "a", "b" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNotNull(command.Spawn(new List<string> { "a", "b", "c" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNull(command.Spawn(new List<string>{"a", "b", "c", "d"} , this.log, null, this.manager, Program.CommandTrigger.Cmd));

      command = new Program.Command("fixed number of args", mock.Provider, "", minArgs: 2, maxArgs: 4);
      Assert.IsNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNotNull(command.Spawn(new List<string> { "a", "b" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNotNull(command.Spawn(new List<string> { "a", "b", "c" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNotNull(command.Spawn(new List<string> { "a", "b", "c", "d" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));
      Assert.IsNull(command.Spawn(new List<string> { "a", "b", "c", "d", "e" }, this.log, null, this.manager, Program.CommandTrigger.Cmd));
    }
   
    public void CheckTrigger() {
      var mock = new CommandLineTest.MockCommand();

      var command = new Program.Command("antenna", mock.Provider, "", requiredTrigger: Program.CommandTrigger.Antenna);
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Antenna));
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.User));
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Cmd));

      command = new Program.Command("user", mock.Provider, "");
      Assert.IsNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Antenna));
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.User));
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Cmd));

      command = new Program.Command("cmd", mock.Provider, "", requiredTrigger: Program.CommandTrigger.Cmd);
      Assert.IsNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Antenna));
      Assert.IsNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.User));
      Assert.IsNotNull(command.Spawn(new List<string>(), this.log, null, this.manager, Program.CommandTrigger.Cmd));
    }

    public void HelpArguments() {
      var mock = new CommandLineTest.MockCommand();
      string log = null;

      var command = new Program.Command("cmd", mock.Provider, "");
      command.DetailedHelp(s => {if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes any number of arguments", log);

      command = new Program.Command("cmd", mock.Provider, "", nArgs: 0);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: (no argument)", log);

      command = new Program.Command("cmd", mock.Provider, "", nArgs: 1);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes 1 argument", log);

      command = new Program.Command("cmd", mock.Provider, "", nArgs: 3);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes 3 arguments", log);

      command = new Program.Command("cmd", mock.Provider, "", minArgs: 2);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes at least 2 arguments", log);

      command = new Program.Command("cmd", mock.Provider, "", maxArgs: 1);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes up to 1 argument", log);

      command = new Program.Command("cmd", mock.Provider, "", maxArgs: 4);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes up to 4 arguments", log);

      command = new Program.Command("cmd", mock.Provider, "", minArgs: 2, maxArgs: 4);
      command.DetailedHelp(s => { if (!string.IsNullOrWhiteSpace(s)) log = s; });
      Assert.AreEqual("-cmd: takes 2-4 arguments", log);
    }
  }
}
