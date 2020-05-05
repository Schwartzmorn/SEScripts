using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IngameScript.MDK {
  class CircularBufferTest {
    public void Everything() {
      var buffer = new Program.CircularBuffer<string>(4);

      Assert.AreEqual(0, buffer.Count);
      Assert.AreEqual(null, buffer.Peek());

      buffer.Enqueue("1");

      Assert.AreEqual(1, buffer.Count);
      Assert.AreEqual("1", buffer.Peek());
      Assert.AreEqual(1, buffer.Count);
      Assert.AreEqual("1", buffer[0]);
      Assert.AreEqual(null, buffer[1]);
      Assert.AreEqual("1", buffer[-1]);
      Assert.AreEqual(null, buffer[-2]);

      Assert.AreEqual("1", string.Join(",", buffer));
      Assert.AreEqual("1", buffer.Dequeue());
      Assert.AreEqual(0, buffer.Count);
      Assert.AreEqual(null, buffer.Peek());
      Assert.AreEqual(null, buffer[0]);
      Assert.AreEqual(null, buffer[-1]);
      Assert.AreEqual(null, buffer.Dequeue());
      Assert.AreEqual("", string.Join(",", buffer));

      for (int i = 1; i < 6; ++i) {
        buffer.Enqueue($"{i}");
      }

      Assert.AreEqual(4, buffer.Count);
      Assert.AreEqual("2", buffer.Peek());
      Assert.AreEqual("2,3,4,5", string.Join(",", buffer));
      Assert.AreEqual("2", buffer[0]);
      Assert.AreEqual("3", buffer[1]);
      Assert.AreEqual("5", buffer[-1]);
      Assert.AreEqual("4", buffer[-2]);
    }
  }
}
