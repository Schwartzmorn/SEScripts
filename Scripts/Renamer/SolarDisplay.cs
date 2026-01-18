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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
  partial class Program
  {
    public class SolarDisplay
    {
      public SolarDisplay(IMyTextPanel panel, bool lazy = true)
      {
        _lazy = lazy;
        _initPanel(panel);
      }
      public void Write(string text)
      {
        _displayText.Append(text).Append("\n");
        if (!_lazy)
        {
          _panel?.WriteText(_displayText, true);
        }
      }
      public void Flush()
      {
        _panel?.WriteText(_displayText);
        _displayText.Clear();
      }
      public void DrawRatio(float operand, float dividend, bool reverse = false)
      {
        float ratio = dividend == 0 ? 1 : Math.Min(1, Math.Max(0, operand / dividend));
        int numberSquares = (int)Math.Round(ratio * COLOR_LINE_LENGTH);
        int colorIndex = Math.Min((int)Math.Floor(ratio * RATIO_COLORS.Count()), RATIO_COLORS.Count() - 1);
        LCDColor color = reverse ? RATIO_COLORS[RATIO_COLORS.Count() - colorIndex - 1] : RATIO_COLORS[colorIndex];
        _displayText
          .Append(color.Char, numberSquares)
          .Append(LCDColor.DARK_GREY.Char, COLOR_LINE_LENGTH - numberSquares)
          .Append('\n');
      }
      public void DisplayTest()
      {
        DrawRatio(0, 1);
        DrawRatio(0.2f, 1);
        DrawRatio(0.4f, 1);
        DrawRatio(0.6f, 1);
        DrawRatio(0.8f, 1);
        DrawRatio(1, 1);
        DrawRatio(0, 1, true);
        DrawRatio(1, 1, true);
        for (int i = 0; i < (LINE_LENGTH + 9) / 10; ++i)
        {
          _displayText.Append("1234567890");
        }
        _displayText.Append("\n");
        for (int i = 0; i < (COLOR_LINE_LENGTH + 1) / 2; ++i)
        {
          _displayText.Append(LCDColor.RED.Char).Append(LCDColor.GREEN.Char);
        }
        _displayText.Append("\n");
        for (int i = 0; i < COLOR_LINE_LENGTH; ++i)
        {
          if ((i + 1) % 10 == 0)
          {
            _displayText.Append(LCDColor.GREEN.Char);
            ECHO("Pouet");
          }
          else
          {
            _displayText.Append(LCDColor.RED.Char);
            ECHO("Prout");
          }
        }
        _displayText.Append("\n");
        for (byte i = 0; i < 8; ++i)
        {
          _displayText.Append(new LCDColor(i, i, i).Char);
        }
        _displayText.Append("\n");
        for (byte i = 0; i < 8; ++i)
        {
          _displayText.Append(new LCDColor(0, 0, i).Char);
        }
        _displayText.Append("\n");
        for (byte i = 0; i < 8; ++i)
        {
          _displayText.Append(new LCDColor(0, 1, i).Char);
        }
        _displayText.Append("\n");
        for (byte i = 0; i < 8; ++i)
        {
          _displayText.Append(new LCDColor(1, 0, i).Char);
        }
        _displayText.Append("\n");
        for (byte i = 0; i < 8; ++i)
        {
          _displayText.Append(new LCDColor(1, 1, i).Char);
        }
        _displayText.Append("\n");
      }
      public void WriteCentered(string text, LCDColor color = null)
      {
        if (text.Count() >= LINE_LENGTH)
        {
          _displayText.Append(text);
        }
        else if (text.Count() >= LINE_LENGTH - 2)
        {
          _displayText.Append(" ").Append(text);
        }
        else
        {
          int remaingColorChars = (int)Math.Round((float)(LINE_LENGTH - text.Count() - 2) * CHAR_COLOR_RATIO / 2);
          _displayText
            .Append(color == null ? LCDColor.BLACK.Char : color.Char, remaingColorChars)
            .Append(" ").Append(text).Append(" ")
            .Append(color == null ? LCDColor.BLACK.Char : color.Char, remaingColorChars + 1);
        }
        _displayText.Append("\n");
      }
      public static string FormatRatio(float operand, float dividend) => operand.ToString("F2") + "MW / " + dividend.ToString("F2") + "MW (" + (100 * operand / dividend).ToString("F1") + "%)";
      private void _initPanel(IMyTextPanel panel)
      {
        _panel = panel;
        _panel.WritePublicTitle("Solar panel output");
        _panel.Font = "Monospace";
        _panel.FontSize = (float)FONT_SIZE;
        _panel.ContentType = ContentType.TEXT_AND_IMAGE;
      }
      static float FONT_SIZE = 0.5f;
      static int LINE_LENGTH = (int)Math.Round(52 / FONT_SIZE);
      static int COLOR_LINE_LENGTH = (int)Math.Round(36 / FONT_SIZE);
      static float CHAR_COLOR_RATIO = ((float)COLOR_LINE_LENGTH) / LINE_LENGTH;
      static List<LCDColor> RATIO_COLORS = new List<LCDColor> { LCDColor.RED, LCDColor.ORANGE, LCDColor.YELLOW, LCDColor.GREEN };
      private IMyTextPanel _panel;
      private bool _lazy;
      StringBuilder _displayText = new StringBuilder();
    }
  }
}