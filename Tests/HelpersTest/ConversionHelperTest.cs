namespace HelpersTest;

using IngameScript;
using NUnit.Framework;

[TestFixture]
public class ConversionHelperTest
{
  [Test]
  public void GetAmount_Works()
  {
    Assert.That("100 W".GetAmount(), Is.EqualTo(100));
    Assert.That("1KW".GetAmount(), Is.EqualTo(1_000));
    Assert.That("1,000 KW".GetAmount(), Is.EqualTo(1_000_000));
    Assert.That("1.5 MW".GetAmount(), Is.EqualTo(1_500_000));
  }

  [Test]
  public void FormatAmount_Works()
  {
    Assert.That(100f.FormatAmount("W"), Is.EqualTo("100 W"));
    Assert.That(1_500f.FormatAmount("W"), Is.EqualTo("1.5 KW"));
    Assert.That(3_000_000f.FormatAmount("Wh"), Is.EqualTo("3 MWh"));
    Assert.That(3_123_456f.FormatAmount("Wh"), Is.EqualTo("3.12 MWh"));
  }
}
