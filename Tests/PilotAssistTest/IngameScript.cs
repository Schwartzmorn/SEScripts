namespace IngameScript;

using System.Collections.Generic;
using System.Linq;

partial class Program
{

  // Mock
  public class WheelsController
  {
    readonly List<float> _powers = [];
    readonly List<float> _steers = [];
    public float Power => _powers.Last();
    public float Steer => _steers.Last();
    public void SetPower(float power) => _powers.Add(power);
    public void SetSteer(float steer) => _steers.Add(steer);
  }

  public class MockDeactivator : Program.IPADeactivator
  {
    public bool Deactivate = false;
    public bool ShouldDeactivate() => Deactivate;
  }

  public class MockBraker : Program.IPABraker
  {
    public bool Handbrake = false;
    public bool ShouldHandbrake() => Handbrake;
  }

}