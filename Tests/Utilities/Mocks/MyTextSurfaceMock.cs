using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Utilities.Mocks;

public class MyTextSurfaceMock : IMyTextSurface
{
  private readonly static List<string> FONTS = ["Debug", "Red", "Green", "Blue", "White", "DarkBlue", "UrlNormal", "UrlHighlight", "ErrorMessageBoxCaption", "ErrorMessageBoxText", "InfoMessageBoxCaption", "InfoMessageBoxText", "ScreenCaption", "GameCredits", "LoadingScreen", "BuildInfo", "BuildInfoHighlight", "Monospace"];
  private readonly static List<string> SCRIPTS = ["TSS_ClockAnalog", "TSS_ArtificialHorizon", "TSS_ClockDigital", "TSS_EnergyHydrogen", "TSS_FactionIcon", "TSS_Gravity", "TSS_TargetingInfo", "TSS_Velocity", "TSS_VendingMachine", "TSS_Weather", "TSS_Jukebox"];
  // Partial list
  private readonly static List<string> SPRITES = ["Offline", "Offline_wide", "Online", "Online_wide", "Arrow", "Cross", "Danger", "No Entry", "Construction", "White screen", "Grid", "DecorativeBracketLeft", "DecorativeBracketRight", "SquareTapered", "SquareSimple", "IconEnergy", "IconHydrogen", "IconOxygen", "IconTemperature", "AH_GravityHudNegativeDegrees", "AH_GravityHudPositiveDegrees", "AH_TextBox", "AH_PullUp", "AH_VelocityVector", "AH_BoreSight", "RightTriangle", "Triangle", "Circle", "SemiCircle", "CircleHollow", "SquareHollow", "UVChecker", "OutOfOrder", "StoreBlock2"];

  public string WrittenText { get; private set; } = "";

  public string CurrentlyShownImage => throw new NotImplementedException();

  public float FontSize { get; set; } = 1;
  public Color FontColor { get; set; } = new Color(255, 255, 255);
  public Color BackgroundColor { get; set; } = new Color(0, 0, 0);
  public byte BackgroundAlpha { get; set; } = 0;
  public float ChangeInterval { get; set; }
  public string Font { get; set; } = "Debug";
  public TextAlignment Alignment { get; set; } = TextAlignment.LEFT;
  public string Script { get; set; } = "";
  public ContentType ContentType { get; set; } = ContentType.NONE;

  public Vector2 SurfaceSize { get; init; } = new Vector2(100, 100);

  public Vector2 TextureSize { get; init; } = new Vector2(100, 100);

  public bool PreserveAspectRatio { get; set; } = true;
  public float TextPadding { get; set; } = 0f;
  public Color ScriptBackgroundColor { get; set; } = new Color(255, 255, 255);
  public Color ScriptForegroundColor { get; set; } = new Color(0, 0, 0);

  public string Name { get; init; } = "MainScreen";

  public string DisplayName { get; init; } = "Large Display";

  public List<MySprite> LastDrawnSprites { get; } = [];

  public void AddImagesToSelection(List<string> ids, bool checkExistence = false)
  {
    throw new NotImplementedException();
  }

  public void AddImageToSelection(string id, bool checkExistence = false)
  {
    throw new NotImplementedException();
  }

  public void ClearImagesFromSelection()
  {
    throw new NotImplementedException();
  }

  public MySpriteDrawFrame DrawFrame()
  {
    LastDrawnSprites.Clear();
    return new MySpriteDrawFrame(frame => frame.AddToList(LastDrawnSprites));
  }

  public void GetFonts(List<string> fonts)
  {
    fonts.AddRange(FONTS);
  }

  public void GetScripts(List<string> scripts)
  {
    scripts.AddRange(SCRIPTS);
  }

  public void GetSelectedImages(List<string> output)
  {
    throw new NotImplementedException();
  }

  public void GetSprites(List<string> sprites)
  {
    sprites.AddRange(SPRITES);
  }

  public string GetText()
  {
    return WrittenText;
  }

  public Vector2 MeasureStringInPixels(StringBuilder text, string font, float scale)
  {
    // not trying to be accurate, empirically modelled after Monospace in Space Engineers
    var lines = text.ToString().Split("\n");

    var maxLength = lines.Select(s => s.Length).Max();

    return new Vector2(
      Math.Max(0, ((maxLength * 19f) - 2) * scale),
      ((lines.Length * 30f) - 2) * scale
    );
  }

  public void ReadText(StringBuilder buffer, bool append = false)
  {
    if (!append)
    {
      buffer.Clear();
    }
    buffer.Append(WrittenText);
  }

  public void RemoveImageFromSelection(string id, bool removeDuplicates = false)
  {
    throw new NotImplementedException();
  }

  public void RemoveImagesFromSelection(List<string> ids, bool removeDuplicates = false)
  {
    throw new NotImplementedException();
  }

  public bool WriteText(string value, bool append = false)
  {
    if (append)
    {
      WrittenText += value;
    }
    else
    {
      WrittenText = value;
    }
    return true;
  }

  public bool WriteText(StringBuilder value, bool append = false)
  {
    if (append)
    {
      WrittenText += value.ToString();
    }
    else
    {
      WrittenText = value.ToString();
    }
    return true;
  }
}
