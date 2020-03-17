using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program {
    /// <summary>Interface to implement to be able to automatically disable the pilot assistance</summary>
    public interface IPADeactivator { bool ShouldDeactivate(); }
    /// <summary>Interface to implement to be able to automatically engage brakes</summary>
    public interface IPABraker { bool ShouldHandbrake(); }
    /// <summary>
    /// <para>Class that automatically engages the hand brake when there is no pilot and allows finer wheel control with a pad.</para>
    /// <para>It maintains a list of controllers, and if there is no pilot in any of the controllers, it engages the handbrake.</para>
    /// </summary>
    public class PilotAssist: IIniConsumer {
      const string SECTION = "pilot-assist";

      public bool ManuallyBraked { get; private set; }
      bool Braked => this.controllers.First().HandBrake;
      bool Deactivated => this.deactivators.Any(d => d.ShouldDeactivate());

      readonly List<IMyShipController> controllers = new List<IMyShipController>(3);
      readonly List<IPADeactivator> deactivators = new List<IPADeactivator>();
      readonly List<IPABraker> handBrakers = new List<IPABraker>();
      readonly IMyGridTerminalSystem gts;
      readonly Action<string> logger;
      readonly WheelsController wheelControllers;
      bool assist;
      float sensitivity;
      bool wasPreviouslyAutoBraked;
      /// <summary>Creates a PilotAssist</summary>
      /// <param name="gts">To get the different blocks</param>
      /// <param name="ini">Parsed ini that contains the configuration. See <see cref="Read(Ini)"/> for more information</param>
      /// <param name="logger">Optional logger</param>
      /// <param name="spawner">Used to schedule itself</param>
      /// <param name="wc">Wheel controller used to actually controll the wheels</param>
      public PilotAssist(IMyGridTerminalSystem gts, MyIni ini, Action<string> logger, ISaveManager spawner, WheelsController wc) {
        this.logger = logger;
        this.wheelControllers = wc;
        this.gts = gts;
        this.Read(ini);
        this.ManuallyBraked = !this.shouldBrake() && this.Braked;
        this.wasPreviouslyAutoBraked = this.shouldBrake();
        spawner.Spawn(this.handle, "pilot-assist");
        spawner.AddOnSave(this.save);
      }
      /// <summary>Adds an object that is polled to see if the automatic handbrake should be engaged, on top of the default behaviour.</summary>
      /// <param name="d">Deactivator. If any registered braker returns true, the automatic brakes are engaged, unless deactivated.</param>
      public void AddBraker(IPABraker h) => this.handBrakers.Add(h);
      /// <summary>Adds an object that is polled to see if the automatic handbrake should be deactivated.</summary>
      /// <param name="d">Deactivator. If any registered deactivator returns true, the automatic brakes are disengaged.</param>
      public void AddDeactivator(IPADeactivator d) => this.deactivators.Add(d);
      /// <summary>
      /// Can be called to update the configuration without restarting the program. Called at creation time.
      /// <para>The ini must contain a section named <see cref="SECTION"/> which contains:</para>
      /// <list type="bullet">
      /// <item><b>assist</b>: whether the fine controls are active: it overrides the normal controls.</item>
      /// <item><b>controllers</b>: names of the controllers considered by pilot assist. At least one valid controller is needed.</item>
      /// <item><b>sensitivity</b>: maximum value the controller input can take. The higher the less sensitive</item>
      /// </list>
      /// </summary>
      /// <param name="ini">The configuration</param>
      public void Read(MyIni ini) {
        this.assist = ini.Get(SECTION, "assist").ToBoolean();
        this.controllers.Clear();
        this.sensitivity = ini.Get(SECTION, "sensitivity").ToSingle(2f);
        string[] names = ini.GetThrow(SECTION, "controllers").ToString().Split(new char[] { ',' });
        foreach (string s in names) {
          var cont = this.gts.GetBlockWithName(s) as IMyShipController;
          if (cont != null) {
            cont.ControlWheels = !this.assist;
            this.controllers.Add(cont);
          } else {
            this.logger?.Invoke($"Could not find ship controller {s}");
          }
        }
        if (this.controllers.Count == 0) {
          throw new InvalidOperationException("No controller found");
        }
      }
      void handle(Process p) {
        if (!this.Deactivated) {
          if (!this.wasPreviouslyAutoBraked && this.Braked) {
            this.ManuallyBraked = true;
          } else if (this.ManuallyBraked && !this.Braked) {
            this.ManuallyBraked = false;
          }
          this.wasPreviouslyAutoBraked = this.shouldBrake();
          this.controllers.First().HandBrake = this.wasPreviouslyAutoBraked || this.ManuallyBraked;
          if (this.assist) {
            this.wheelControllers.SetPower(-this.controllers.Sum(c => c.MoveIndicator.Z) / this.sensitivity);
            float right = this.controllers.Sum(c => c.MoveIndicator.X);
            this.wheelControllers.SetSteer(right == 1 ? 1 : right / this.sensitivity);
          }
        }
      }
      void save(MyIni ini) {
        ini.Set(SECTION, "assist", this.assist);
        ini.Set(SECTION, "controllers", string.Join(",", this.controllers.Select(c => c.DisplayNameText)));
        ini.Set(SECTION, "sensitivity", this.sensitivity);
      }
      bool shouldBrake() {
        // To engage the handbrake when the pilot presses the up key (esp. useful when controlling with the pad)
        float up = this.assist ? this.controllers.Sum(c => c.MoveIndicator.Y) : 0;
        return this.controllers.All(c => !c.IsUnderControl) || this.handBrakers.Any(h => h.ShouldHandbrake()) || up == 1;
      }
    }
  }
}
