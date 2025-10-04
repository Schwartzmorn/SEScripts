namespace LoggerTest;

using IngameScript;
using NUnit.Framework;

[TestFixture]
class CircularBufferTest
{
  [Test]
  public void It_Works()
  {
    var buffer = new Program.CircularBuffer<string>(4);

    Assert.That(buffer.Count, Is.EqualTo(0));
    Assert.That(buffer.Peek(), Is.Null);

    buffer.Enqueue("1");

    Assert.That(buffer.Count, Is.EqualTo(1));
    Assert.That(buffer.Peek(), Is.EqualTo("1"));
    Assert.That(buffer.Count, Is.EqualTo(1));
    Assert.That(buffer[0], Is.EqualTo("1"));
    Assert.That(buffer[1], Is.Null);
    Assert.That(buffer[-1], Is.EqualTo("1"));
    Assert.That(buffer[-2], Is.Null);

    Assert.That(string.Join(",", buffer), Is.EqualTo("1"));
    Assert.That(buffer.Dequeue(), Is.EqualTo("1"));
    Assert.That(buffer.Count, Is.EqualTo(0));
    Assert.That(buffer.Peek(), Is.Null);
    Assert.That(buffer[0], Is.Null);
    Assert.That(buffer[-1], Is.Null);
    Assert.That(buffer.Dequeue(), Is.Null);
    Assert.That(string.Join(",", buffer), Is.EqualTo(""));

    for (int i = 1; i < 6; ++i)
    {
      buffer.Enqueue($"{i}");
    }

    Assert.That(buffer.Count, Is.EqualTo(4));
    Assert.That(buffer.Peek(), Is.EqualTo("2"));
    Assert.That(string.Join(",", buffer), Is.EqualTo("2,3,4,5"));
    Assert.That(buffer[0], Is.EqualTo("2"));
    Assert.That(buffer[1], Is.EqualTo("3"));
    Assert.That(buffer[-1], Is.EqualTo("5"));
    Assert.That(buffer[-2], Is.EqualTo("4"));
  }
}
