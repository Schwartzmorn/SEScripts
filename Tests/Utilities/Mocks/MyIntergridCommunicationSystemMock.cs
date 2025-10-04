namespace Utilities.Mocks;

using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage;

public class MyIntergridCommunicationSystemMock(TestBed testBed) : IMyIntergridCommunicationSystem
{
  public readonly TestBed TestBed = testBed;

  public long Me { get; } = testBed.GetNextEntityId();

  public readonly MyUnicastListenerMock UnicastListenerMock = new();

  public IMyUnicastListener UnicastListener => UnicastListenerMock;

  public MyTuple<string, object, TransmissionDistance> LastBroadcastMessage => SentBrodcastMessages.Last();

  public readonly List<MyTuple<string, object, TransmissionDistance>> SentBrodcastMessages = [];

  public void DisableBroadcastListener(IMyBroadcastListener broadcastListener)
  {
    throw new NotImplementedException();
  }

  public void GetBroadcastListeners(List<IMyBroadcastListener> broadcastListeners, Func<IMyBroadcastListener, bool> collect = null)
  {
    throw new NotImplementedException();
  }

  public bool IsEndpointReachable(long address, TransmissionDistance transmissionDistance = TransmissionDistance.AntennaRelay)
  {
    throw new NotImplementedException();
  }

  public IMyBroadcastListener RegisterBroadcastListener(string tag)
  {
    throw new NotImplementedException();
  }

  public void SendBroadcastMessage<TData>(string tag, TData data, TransmissionDistance transmissionDistance = TransmissionDistance.AntennaRelay)
  {
    SentBrodcastMessages.Add(new MyTuple<string, object, TransmissionDistance>(tag, data, transmissionDistance));
  }

  public bool SendUnicastMessage<TData>(long addressee, string tag, TData data)
  {
    throw new NotImplementedException();
  }
}
