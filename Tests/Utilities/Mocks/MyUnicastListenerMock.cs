namespace Utilities.Mocks;

using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

public class MyUnicastListenerMock : IMyUnicastListener
{
  readonly Queue<MyIGCMessage> _messages = [];

  public bool HasPendingMessage => _messages.Count > 0;

  public int MaxWaitingMessages => throw new System.NotImplementedException();

  public MyIGCMessage AcceptMessage()
  {
    return _messages.Dequeue();
  }

  public void DisableMessageCallback()
  {
    throw new System.NotImplementedException();
  }

  public void SetMessageCallback(string argument = "")
  {
    throw new System.NotImplementedException();
  }

  public void QueueMessage<TData>(TData data, string tag = "whatever", long source = -1)
  {
    _messages.Enqueue(new MyIGCMessage(data, tag, source));
  }
}
