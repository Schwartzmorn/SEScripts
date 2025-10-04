namespace Utilities.Mocks.Base;

using System;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;

public class TerminalPropertyMock<TBlock, T> : ITerminalProperty<T>
    where TBlock : MyCubeBlockMock
{
  readonly Func<TBlock, T> _getter;
  readonly Action<TBlock, T> _setter;
  readonly T _defaultValue;
  readonly Func<TBlock, T> _getMin;
  readonly Func<TBlock, T> _getMax;

  public TerminalPropertyMock(string name, Func<TBlock, T> getter, Action<TBlock, T> setter, T defaultValue = default, Func<TBlock, T> getMin = null, Func<TBlock, T> getMax = null)
  {
    ArgumentNullException.ThrowIfNull(getter);

    ArgumentNullException.ThrowIfNull(setter);

    if (string.IsNullOrEmpty(name))
      throw new ArgumentException("Value cannot be null or empty.", nameof(name));

    Id = name;
    _getter = getter;
    _setter = setter;
    _defaultValue = defaultValue;
    _getMin = getMin;
    _getMax = getMax;
  }

  public string Id { get; }

  public string TypeName => typeof(T).Name;

  public virtual T GetDefaultValue(IMyCubeBlock block) => _defaultValue;

  public virtual T GetMaximum(IMyCubeBlock block)
  {
    if (_getMax != null)
      return _getMax((TBlock)block);
    return default;
  }

  T ITerminalProperty<T>.GetMininum(IMyCubeBlock block) => GetMinimum(block);

  public virtual T GetMinimum(IMyCubeBlock block)
  {
    if (_getMin != null)
      return _getMin((TBlock)block);
    return default;
  }

  public virtual T GetValue(IMyCubeBlock block)
  {
    return _getter((TBlock)block);
  }

  public void SetValue(IMyCubeBlock block, T value)
  {
    _setter((TBlock)block, value);
  }
}
