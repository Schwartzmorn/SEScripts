using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public partial class Marshaller {
      public class TestMarshaller : TestSuite.Test {
        public override void DoTests() {
          Marshaller marshaller = new Marshaller();
          marshaller.Marshall("string1");
          marshaller.Marshall(1);
          marshaller.Marshall(10.1);
          AssertEqual(marshaller.GetStorage(), "string1;1;10.1;", "Wrong marshalling");
          marshaller = new Marshaller();
          marshaller.MarshallList(new List<double> { 1, 10.1 });
          AssertEqual(marshaller.GetStorage(), "2;1;10.1;", "Wrong marshalling of list");
        }
      }
    }
    public partial class Unmarshaller {
      public class TestUnmarshaller : TestSuite.Test {
        public override void DoTests() {
          Unmarshaller unmarshaller = new Unmarshaller("string1;string2;");
          AssertEqual(unmarshaller._storage.Count(), 2, "Wrong number of strings unmarshalled");
          AssertEqual(unmarshaller._index, 0, "Wrong initial index");
          AssertEqual(unmarshaller.UnmarshallString(), "string1", "Wrong first string unmarshalling");
          AssertEqual(unmarshaller._index, 1, "Wrong updated index");
          AssertEqual(unmarshaller.UnmarshallString(), "string2", "Wrong second string unmarshalling");
          AssertEqual(unmarshaller.UnmarshallString(), "", "Wrong default string unmarshalling");
          unmarshaller = new Unmarshaller("1;");
          AssertEqual(unmarshaller._storage.Count(), 1, "Wrong number of int unmarshalled");
          AssertEqual(unmarshaller.UnmarshallInt(), 1, "Wrong int unmarshalling");
          AssertEqual(unmarshaller.UnmarshallInt(), 0, "Wrong default int unmarshalling");
          unmarshaller = new Unmarshaller("1000;");
          AssertEqual(unmarshaller._storage.Count(), 1, "Wrong number of double unmarshalled");
          AssertEqual(unmarshaller.UnmarshallDouble(), 1000, "Wrong double unmarshalling");
          AssertEqual(unmarshaller.UnmarshallDouble(), 0, "Wrong default double unmarshalling");
        }
      }
    }
  }
}
