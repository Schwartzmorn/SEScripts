namespace Utilities.Mocks.Base;

using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

// TODO implement a way to add surfaces
public class MyTextSurfaceProviderImpl
{
  private readonly List<IMyTextSurface> _surfaces = [];

  public int Count => _surfaces.Count;

  public bool UseGenericLcd { get; set; } = false;

  public IMyTextSurface GetSurface(int index) => index < _surfaces.Count ? _surfaces[index] : null;
}

public interface IMyTextSurfaceProviderMock : IMyTextSurfaceProvider
{
  MyTextSurfaceProviderImpl SurfaceProviderImpl { get; }

  int IMyTextSurfaceProvider.SurfaceCount { get => SurfaceProviderImpl.Count; }

  bool IMyTextSurfaceProvider.UseGenericLcd { get => SurfaceProviderImpl.UseGenericLcd; }

  IMyTextSurface IMyTextSurfaceProvider.GetSurface(int index) => SurfaceProviderImpl.GetSurface(index);
}
