namespace Utilities.Mocks;

using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

public class MyBroadcastListenerMock : IMyBroadcastListener
{
  readonly Queue<MyIGCMessage> _messages = [];

  public string Tag { get; init; }

  public bool IsActive => throw new System.NotImplementedException();

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
}
