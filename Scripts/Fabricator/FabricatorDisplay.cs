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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
  partial class Program {
    public class FabricatorDisplay {
      readonly Display mandatoryDisplay;
      readonly Display otherDisplay;
      readonly Display statusDisplay;
      readonly InventoryManager invManager;
      readonly Projector projector;
      readonly Welder welder;
      readonly string name;
      readonly Process mainProcess;
      string lastMessage;
      int counter = 0;
      float completion;

      public string LastMessage {
        get { return this.lastMessage; }
        set {
          this.lastMessage = value;
          this.mainProcess.KillChildren();
          this.mainProcess.Spawn(p => this.lastMessage = null, "message-cleanup", period: 800, useOnce: true);
        }
      }

      static readonly ColorScheme SCHEME = new ColorScheme();
      static readonly Color CRITICAL = new Color(255, 0, 0);
      static readonly Color ERROR = new Color(255, 127, 0);
      static readonly Color WARNING = new Color(255, 255, 0);

      public FabricatorDisplay(IMyTextSurface statusDisplay, IMyTextSurface mandatoryDisplay, IMyTextSurface otherDisplay, string name, Projector projector, Welder welder, InventoryManager invManager, IProcessSpawner spawner) {
        this.statusDisplay = new Display(statusDisplay);
        this.mandatoryDisplay = new Display(mandatoryDisplay);
        this.otherDisplay = new Display(otherDisplay);
        this.invManager = invManager;
        this.name = name;
        this.welder = welder;
        this.projector = projector;
        this.mainProcess = spawner.Spawn(p => this.updateDisplays(), "display-process", period: 30);
      }

      void updateDisplays() {
        if (this.projector.GetStatus() == ProjectorStatus.Projecting) {
          float completion = this.projector.GetCompletion();
          if (this.completion == completion) {
            ++this.counter;
          } else {
            this.completion = completion;
            this.counter = 0;
          }
        } else {
          this.completion = 0;
          this.counter = -1;
        }
        this.updateInventoryDisplay(this.mandatoryDisplay, "Mandatory items status", this.invManager.MandatoryItems);
        this.updateInventoryDisplay(this.otherDisplay, "Other items status", this.invManager.OtherItems);
        this.updateStatusDisplay();
      }

      void updateInventoryDisplay(Display display, string title, List<ComponentStatus> statuses) {
        using (Display.Frame f = display.DrawFrame()) {
          f.DrawText(title, new Vector2(display.SurfaceSize.X / 2, 0));
          float y = 50;
          foreach (ComponentStatus status in statuses) {
            Color? color = status.Status == ComponentStatusLevel.ERROR
                ? CRITICAL
                : status.Status == ComponentStatusLevel.WARNING
                    ? WARNING
                    : (Color?)null;
            this.drawLineOfText(f, status.Type.DisplayName, $"{status.Amount:N0}", display.SurfaceSize.X - 2, ref y, color);
          }
        }
      }

      void updateStatusDisplay() {
        using (Display.Frame f = this.statusDisplay.DrawFrame()) {
          f.DrawText(this.name + " status", new Vector2(this.statusDisplay.SurfaceSize.X / 2, 0));
          float x = this.statusDisplay.SurfaceSize.X - 2;
          float y = 60;

          this.drawLine(f, this.statusDisplay.SurfaceSize.X, ref y);

          ProjectorStatus projStatus = this.projector.GetStatus();
          WelderStatus welderStatus = this.welder.GetStatus();
          InventoryStatus invStatus = this.invManager.GetStatus();

          if (projStatus == ProjectorStatus.Projecting &&
              welderStatus == WelderStatus.Deployed &&
              invStatus != InventoryStatus.NotReady) {
            this.drawLineOfText(f, "Status", "Ready", x, ref y);
          } else {
            this.drawLineOfText(f, "Status", "Not ready", x, ref y, CRITICAL);
          }

          if (projStatus == ProjectorStatus.Projecting) {
            this.drawLineOfText(f, "Completion", $"{this.projector.GetCompletion() * 100:N0}%", x, ref y);
            this.drawLineOfText(f, "Time since block added", $"{this.counter}", x, ref y);
          } else {
            this.drawLineOfText(f, "Completion", "N/A", x, ref y);
            this.drawLineOfText(f, "Time since block added", "N/A", x, ref y);
          }

          this.drawLine(f, this.statusDisplay.SurfaceSize.X, ref y);

          Color? color = projStatus != ProjectorStatus.Projecting
              ? CRITICAL
              : (Color?)null;
          this.drawLineOfText(f, "Projector status", projStatus.ToString(), x, ref y, color);
          color = this.projector.HasBlocksAttached()
              ? WARNING
              : (Color?)null;
          this.drawLineOfText(f, "Has blocks attached", this.projector.HasBlocksAttached().ToString(), x, ref y, color);

          this.drawLine(f, this.statusDisplay.SurfaceSize.X, ref y);

          color = welderStatus != WelderStatus.Deployed
              ? CRITICAL
              : (Color?)null;
          this.drawLineOfText(f, "Welder status", welderStatus.ToString(), x, ref y, color);

          this.drawLine(f, this.statusDisplay.SurfaceSize.X, ref y);

          color = invStatus == InventoryStatus.NotReady
              ? CRITICAL
              : invStatus == InventoryStatus.MandatoryLow
                  ? ERROR
                  : invStatus == InventoryStatus.OptionalLow
                      ? WARNING
                      : (Color?)null;
          this.drawLineOfText(f, "Inventory status", this.getString(invStatus), x, ref y, color);

          this.drawLine(f, this.statusDisplay.SurfaceSize.X, ref y);

          if (!string.IsNullOrEmpty(this.LastMessage)) {
            f.DrawText(this.LastMessage, new Vector2(0, y), CRITICAL, 0.75f, alignment: TextAlignment.LEFT);
          }
        }
      }

      void drawLineOfText(Display.Frame f, string left, string right, float x, ref float y, Color? color = null) {
        f.DrawText(left, new Vector2(0, y), color, 0.75f, TextAlignment.LEFT);
        f.DrawText(right, new Vector2(x, y), color, 0.75f, TextAlignment.RIGHT);
        y += 30;
      }
      string getString(InventoryStatus status) {
        switch (status) {
          case InventoryStatus.Nominal:
            return "Nominal";
          case InventoryStatus.MandatoryLow:
            return "Low";
          case InventoryStatus.OptionalLow:
            return "Optional low";
          default:
            return "Not ready";
        }
      }

      void drawLine(Display.Frame f, float width, ref float y) {
        y += 15;
        f.Draw(new Shape("SquareSimple", SCHEME.Light, new Vector2(0, y), new Vector2(width, 2)));
        y += 20;
      }
    }
  }
}
