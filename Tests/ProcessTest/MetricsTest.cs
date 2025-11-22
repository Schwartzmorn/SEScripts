namespace ProcessTest;

using FakeItEasy;
using IngameScript;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
class MetricsTest
{
  [Test]
  public void Metrics_Can_Be_Added()
  {
    var m = new Program.Metrics();

    m.Increment("test", "zut", 10);
    m.Increment("test", "flute", 10);
    m.Increment("test", "zut", 30);
    m.Set("test", "flute", 5);

    Assert.That(m.Get("test", "zut"), Is.EqualTo(40));
    Assert.That(m.Get("test", "flute"), Is.EqualTo(5));
  }
}
