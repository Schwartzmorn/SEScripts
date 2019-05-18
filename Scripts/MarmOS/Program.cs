﻿using Sandbox.Game.EntityComponents;
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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
  partial class Program : MyGridProgram {

    void MArmOS_Configuration() {
      var RZ = new Rotor(Name: "W1 Rotor Z", Axis: "Z");

      var PH1L = new Piston(Name: "W1 Piston H1L", Axis: "X", Home: 0.075, MaxSpeed: 0.1);
      var PH1R = new Piston(Name: "W1 Piston H1R", Axis: "X", Home: 0.075, MaxSpeed: 0.1);
      var PH2 = new Piston(Name: "W1 Piston H2", Axis: "X", Home: 0.075, MaxSpeed: 0.1);
      var SH = new SolidSG(1, 0, -0.5);

      var HY = new Hydraulic(
        Actuator: (PH1L * PH1R) + PH2 + SH,
        Axis: "Y",
        Tangent1: 1.5,
        Normal1: 2,
        Tangent2: 0,
        Normal2: 2.5,
        Home: 22
      );

      var SX1 = new SolidSG(16, 0, 0);

      var RY2L = new Rotor(Name: "W1 Rotor Y2L", Axis: "Y", Home: 162);
      var RY2R = new Rotor(Name: "W1 Rotor Y2R", Axis: "-Y", Home: -162);

      var SX2 = new SolidSG(15, 0, 0);

      var RY3 = new Rotor(Name: "W1 Rotor Y3", Axis: "Y", Home: -162);

      var SX3 = new SolidSG(15, 0, 0);

      var RY4 = new Rotor(Name: "W1 Rotor Y4", Axis: "-Y", Home: -175);

      var SX4 = new SolidSG(7, 0, 0);

      var arm = RZ + HY + SX1 + (RY2L * RY2R) + SX2 + RY3 + SX3 + RY4 + SX4;

      new UserControl(
        Arm: arm,
        ReferenceFrame: arm,
        Speed: 5,
        ShipControllerKeyword: "W1 Arm Controller",
        ReadMouse: false);

      // connector initialization
      var cockpit = GridTerminalSystem.GetBlockWithName("W1 Cockpit") as IMyCockpit;
      Logger.SetupGlobalInstance(new Logger(cockpit.GetSurface(0), fontSize: 1), Echo);
      _commandline = new CmdLine("MarmOS", Logger.Inst.Log);
      var ini = new MyIni();
      if(!ini.TryParse(Me.CustomData)) {
        Logger.Inst.Log("Could not parse ini");
      }
      var connector = GridTerminalSystem.GetBlockWithName("W1 Connector (Front)") as IMyShipConnector;
      var connectionRequestor = new ConnectionClient(this, ini, _commandline, "StationConnectionRequests", "W1Connections");
      var autoHandbrake = new AutoHandbrake(ini, GridTerminalSystem);
      autoHandbrake.AddBraker(connectionRequestor);
      var wheelsController = new WheelsController(this, ini, _commandline, new CoordsTransformer(cockpit, true), cockpit);

      _marmosMain = new ScheduledAction(() => {
          MArmOS_Main(_arguments);
          _arguments = "";
        }, period: 10);
      Scheduler.Inst.AddAction(_marmosMain);
      Scheduler.Inst.AddAction(Logger.Flush);
    }

    public void Main(string argument, UpdateType updateSource) {
      _commandline.HandleCmd(argument, true);
      if (argument != "") {
        _arguments = argument;
      }
      Scheduler.Inst.Tick();
    }

    private ScheduledAction _marmosMain;
    private CmdLine _commandline;
    private string _arguments;
    /////////////////////////////////   G L O B A L   S E T T I N G S
    static String DefaultName = "";
    static double DefaultSpeed = 2;
    static double DefaultSoftness = 10;
    static bool DefaultStartOn = true;
    static String DefaultShipControllerKeyword = "Arm Controller";
    bool DefaultReadKeyboard = true;
    bool DefaultReadMouse = true;
    double DefaultYawSpeed = 1;
    double DefaultPitchSpeed = 1;
    double DefaultRollSpeed = 1;
    bool DefaultUseArmAsReference = false;

    static String EchoScreenName = "MArmOS Echo";  // Your arms’ status will be displayed on these panels.
    static String DebugScreenName = "MArmOS Debug";  // Your arms’ debug info will be displayed on these panels.
    static String LogScreenName = "MArmOS Log";  // A short log of the main events will be displayed on these panels.
    static double LG = 2.5;  // The dimension of a large Grid in Meters
    static double SG = 0.5;  // The dimension of a small Grid in Meters
    static double C = 0.5;  // The dimension of a cubits in Meters
    static double HomeSpeedFactor = 1;  // How fast should GoHome be.
    static double HomeSpeedPropagation = 1.2;  // Hardware on the tip goes faster.


    // UPDATE LINE: Everything under this line can be copy/pasted to update versions over v3.0.
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Current Version: 3.0




    /*
      <<  ----  M A R M O S ' S   E N T R A I L S  --------------------------------------------------------------------  >>
      <<  --------------------------------------------------------------------  M A R M O S ' S   E N T R A I L S  ----  >>
    */
    // Nothing should need to be changed below this line. It is the inner working of MArmOS.

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Update1;
      mEcho = Echo;
      MyGTS = GridTerminalSystem;
      Me = Me;
      GlobStep = 1000;
      MyLog("Starting configuration.");
      Controller.MyControllers = new List<Controller>();

      MArmOS_Configuration();
      if (Controller.MyControllers.Count < 1) {
        MyLog("No Controller Defined. Using 'DefaultController' instead.");
        DefaultController = new UserControl(
          Arm: DefaultArm
        , Name: DefaultName
        , Speed: DefaultSpeed
        , Softness: DefaultSoftness
        , StartOn: DefaultStartOn
        , ShipControllerKeyword: DefaultShipControllerKeyword
        , ReadKeyboard: DefaultReadKeyboard
        , ReadMouse: DefaultReadMouse
        , YawSpeed: DefaultYawSpeed
        , PitchSpeed: DefaultPitchSpeed
        , RollSpeed: DefaultRollSpeed
        , UseArmAsReference: DefaultUseArmAsReference
        );
      }

      MyLog("Configuration complete.");
    }

    public void MArmOS_Main(string argument) {
      mEcho = Echo;
      MyGTS = GridTerminalSystem;
      Me = Me;
      if (GlobStep > 1006) {
        SleepMode = true;
        Controller.UpdateControllers(argument);
        if (SleepMode) {
          if (Sleep != SleepMode)
            _marmosMain.Period = 10;
          MyEcho("Script running in Sleep mode for performance friendlyness");
          GlobStep += 9;
        } else {
          if (Sleep != SleepMode)
            _marmosMain.Period = 1;
          MyEcho("Script running at max speed");
        }
        Sleep = SleepMode;
      } else {
        MyEcho("Loading...");
      }
      if (ErrorFlag > GlobStep) MyEcho("Errors detected. Build a panel and name it '" + LogScreenName + "' to display a detailed log.");
      if (WarningFlag > GlobStep) MyEcho("Warnings detected. Build a panel and name it '" + LogScreenName + "' to display a detailed log.");



      GlobStep++;
    }

    // << ---- G L O B A L   V A R I A B L E S ---- >>
    static Dictionary<String, Hardware> HardwareList = new Dictionary<String, Hardware>();
    static System.Action<String> mEcho;  // A pointer to the Echo method
    static IMyGridTerminalSystem MyGTS;  // A pointer to the grid terminal system
                                         //static IMyProgrammableBlock  MyMe;  // A pointer to the Programmable block
    static uint GlobStep;  // The main clock reference
    static Hardware DefaultArm;
    static Controller DefaultController;
    static uint NORMALFPS = 60;
    static double dt = 0.1;  // Derivation constant.
    static bool SleepMode = false;  // Wether or not the script should run in Sleep mode
    static bool Sleep = false;

    // << ---- S Y S T E M   V A R I A B L E S ---- >>
    static List<IMyTerminalBlock> debugPanels = new List<IMyTerminalBlock>();
    static List<IMyTerminalBlock> echoPanels = new List<IMyTerminalBlock>();
    static List<IMyTerminalBlock> logPanels = new List<IMyTerminalBlock>();
    static int LogPointer = 0;
    static uint EchoStep = 0;
    static uint DebugStep = 0;
    static uint LogStep = 0;
    static uint ErrorFlag = 0;
    static uint WarningFlag = 0;

    static List<IMyTerminalBlock> EchoPanels {
      get {
        if (EchoStep < GlobStep - 60 && EchoScreenName != "") {
          MyGTS.SearchBlocksOfName(EchoScreenName, echoPanels);
          EchoStep = GlobStep;
        }
        foreach (IMyTerminalBlock Panel in echoPanels) {
          ((IMyTextPanel)Panel).ContentType = ContentType.TEXT_AND_IMAGE;
        }
        return echoPanels;
      }
    }
    static List<IMyTerminalBlock> DebugPanels {
      get {
        if (DebugStep < GlobStep - 60 && DebugScreenName != "") {
          MyGTS.SearchBlocksOfName(DebugScreenName, debugPanels);
          DebugStep = GlobStep;
        }
        foreach (IMyTerminalBlock Panel in debugPanels) {
          ((IMyTextPanel)Panel).ContentType = ContentType.TEXT_AND_IMAGE;
        }
        return debugPanels;
      }
    }
    static List<IMyTerminalBlock> LogPanels {
      get {
        if (LogStep < GlobStep - 60) {
          MyGTS.SearchBlocksOfName(LogScreenName, logPanels);
          LogStep = GlobStep;
        }
        foreach (IMyTerminalBlock Panel in logPanels) {
          ((IMyTextPanel)Panel).ContentType = ContentType.TEXT_AND_IMAGE;
          ((IMyTextPanel)Panel).SetValue("FontSize", 0.4F);
        }
        return logPanels;
      }
    }

    // << ---- G L O B A L   F U N C T I O N S ---- >>
    static void MyEcho(String Text) {
      mEcho(Text);
      if (EchoStep != GlobStep) {
        foreach (IMyTerminalBlock EchoPanel in EchoPanels) {
          ((IMyTextPanel)EchoPanel).WriteText(TexEcho, false);
        }
        TexEcho = EchoScreenName + ":\n" + Text + "\n";
        EchoStep = GlobStep;
      } else {
        TexEcho += Text + "\n";
      }
    }
    static void MyDebug(String Text) {
      if (DebugStep != GlobStep) {
        foreach (IMyTerminalBlock DebugPanel in DebugPanels) {
          ((IMyTextPanel)DebugPanel).WriteText(TexDebug, false);
        }
        TexDebug = DebugScreenName + ":\n" + Text + "\n";
        DebugStep = GlobStep;
      } else {
        TexDebug += Text + "\n";
      }
    }
    static String TexEcho = "";
    static String TexDebug = "";
    static String[] logs = new String[41];
    static void MyLog(String Text) {
      logs[LogPointer] = Text;
      LogPointer = LogPointer < 40 ? LogPointer + 1 : 0;
      MyEcho("Log: < " + Text + " >");
      Text = "";
      int i = LogPointer;
      do {
        if (logs[i] != null) { Text = Text + "\n" + logs[i]; }
        i = i < 40 ? i + 1 : 0;
      } while (i != LogPointer);
      Text = LogScreenName + ":\n" + Text;
      foreach (IMyTerminalBlock LogPanel in LogPanels) {
        ((IMyTextPanel)LogPanel).WriteText(Text, false);
      }
    }
    static double STD(String txt) => Convert.ToSingle(txt);
    static double Clamp(double value, double Min, double Max) => Math.Max(Min, Math.Min(Max, value));
    static MatrixD RotX(double Ang) => MatrixD.CreateRotationX(Ang);
    static MatrixD RotY(double Ang) => MatrixD.CreateRotationY(Ang);
    static MatrixD RotZ(double Ang) => MatrixD.CreateRotationZ(Ang);

    /*
      <<  ----  H A R D W A R E  --------------------------------------------------------------------  >>           H A R D W A R E
      <<  --------------------------------------------------------------------  H A R D W A R E  ----  >>           H A R D W A R E
    V1.1*/
    class Hardware {
      // << ---- C O N S T R U C T O R ---- >>
      public Hardware() {
        if (GetType() != typeof(Addition)) {
          if (DefaultArm == null) {
            DefaultArm = this;
          } else {
            DefaultArm = DefaultArm + this;
          }
        }
      }

      // << ---- I N T E R F A C E ---- >>
      public virtual cPose GetPose() => pose;
      public virtual cPose GetNextPose() => npose;
      public virtual cPose GetDeltaPose() => dpose;
      public virtual void SetDeltaPose(cPose Target, Hardware Reference) { }
      public virtual void GoHome(double Speed) { }
      public virtual void SetHome() { }
      public virtual void DebugInfo() { }

      // << ---- O P E R A T O R S ---- >>
      static public Hardware operator +(Hardware p1, Hardware p2) {
        if (p1.GetType() == typeof(Addition)) {
          Hardware H1 = ((Addition)p1).H1;
          p1 = ((Addition)p1).H2 + p2;
          return new Addition(H1, p1);
        } else {
          return new Addition(p1, p2);
        }
      }

      static public Hardware operator *(Hardware p1, Hardware p2) {

        return new Multiplication(p1, p2);
      }

      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public uint PoseID, nPoseID, dPoseID = 0;
      public bool Override = false;

      virtual public double Home {
        get { return home; }
        set { home = value; }
      }
      public cPose Pose {
        get {
          if (PoseStep < GlobStep) {
            pose = GetPose();
            PoseStep = GlobStep;
          }
          return pose;
        }
      }
      public cPose nPose {
        get {
          if (nPoseStep < GlobStep || !nPoseUTD || true) {
            npose = GetNextPose();
            nPoseStep = GlobStep;
            nPoseUTD = true;
          }
          return npose;
        }
      }
      public cPose dPose {
        get {
          if (dPoseStep < GlobStep || !dPoseUTD || true) {
            dpose = GetDeltaPose();
            dPoseStep = GlobStep;
            dPoseUTD = true;
          }
          return dpose;
        }
        set {
          //if ( value.Mat != MatrixD.Identity ){
          SetDeltaPose(value, this);
          nPoseUTD = false;
          dPoseUTD = false;
          //}
        }
      }
      // << ---- P U B L I C   F U N C T I O N S ---- >>
      public void Move(cPose Target, Hardware Reference) {
        SetDeltaPose(Target, Reference);
        nPoseUTD = false;
        dPoseUTD = false;
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      // Pose control variables
      cPose pose = new cPose(), npose = new cPose(), dpose = new cPose();
      uint PoseStep = 0, nPoseStep = 0, dPoseStep = 0;
      bool nPoseUTD = false, dPoseUTD = false;
      double home = 0;
    }
    /*
      <<  ----  A D D I T I O N  --------------------------------------------------------------------  >>           A D D I T I O N
      <<  --------------------------------------------------------------------  A D D I T I O N  ----  >>           A D D I T I O N
    V1.1*/
    class Addition : Hardware {
      // << ---- C O N S T R U C T O R ---- >>
      public Addition(Hardware H1, Hardware H2) : base() {
        this.H1 = H1;
        this.H2 = H2;
      }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public Hardware H1, H2;

      // << ---- O V E R R I D E S ---- >>
      public override cPose GetPose() {
        var TH1Pose = H1.Pose;
        var TH2Pose = H2.Pose;
        if (H1.PoseID != H1PoseID || H2.PoseID != H2PoseID) {
          H1PoseID = H1.PoseID;
          H2PoseID = H2.PoseID;
          pose = TH1Pose + TH2Pose;
          PoseID++;
        }
        return pose;
      }
      public override cPose GetNextPose() {
        var TH1nPose = H1.nPose;
        var TH2nPose = H2.nPose;
        if (H1.nPoseID != H1nPoseID || H2.nPoseID != H2nPoseID) {
          H1nPoseID = H1.nPoseID;
          H2nPoseID = H2.nPoseID;
          npose = TH1nPose + TH2nPose;
          nPoseID++;
        }
        return npose;
      }
      public override cPose GetDeltaPose() {
        var TnPose = nPose;
        var TPose = Pose;
        if (nPoseID != nPoseID2 || PoseID != PoseID2) {
          dpose = TnPose - TPose;
          dpose.Pos = TnPose.Pos - TPose.Pos;
          PoseID2 = PoseID;
          nPoseID2 = nPoseID;
          dPoseID++;
        }
        return dpose;
      }
      public override void SetDeltaPose(cPose Value, Hardware Reference) {
        var IdMat = H1.dPose.Mat;
        var IMat = H1.Pose.Mat;
        //var Ori = new cPose( H1.Pose.Ori );
        //var dOri = new cPose( H1.dPose.Ori );
        //var TOri = new cPose( Value.Ori );
        //var H2Target = (-Ori+(TOri-dOri)+Ori);
        var H2Target = (-H1.Pose + (Value - H1.dPose) + H1.Pose);
        var dX = Value.Mat.M41 - IdMat.M41;
        var dY = Value.Mat.M42 - IdMat.M42;
        var dZ = Value.Mat.M43 - IdMat.M43;
        H2Target.Pos = new Vector3D(
          (dX * IMat.M11) + (dY * IMat.M12) + (dZ * IMat.M13),
          (dX * IMat.M21) + (dY * IMat.M22) + (dZ * IMat.M23),
          (dX * IMat.M31) + (dY * IMat.M32) + (dZ * IMat.M33));
        H2.Move(H2Target, H2);
        H1.Move(Value, this);
      }
      public override void GoHome(double Speed) {
        H1.GoHome(Speed * HomeSpeedFactor);
        H2.GoHome(Speed * HomeSpeedPropagation);
      }
      public override void SetHome() {
        H1.SetHome();
        H2.SetHome();
      }
      public override void DebugInfo() {
        H1.DebugInfo();
        H2.DebugInfo();
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      cPose pose = new cPose(), npose = new cPose(), dpose = new cPose();
      uint H1PoseID, H2PoseID, H1nPoseID, H2nPoseID, PoseID2, nPoseID2 = 0;
    }
    /*
      <<  ----  M U L T I P L I C A T I O N  --------------------------------------------------------------------  >>           M U L T I P L I C A T I O N
      <<  --------------------------------------------------------------------  M U L T I P L I C A T I O N  ----  >>           M U L T I P L I C A T I O N
    V1.1*/
    class Multiplication : Hardware {
      // << ---- C O N S T R U C T O R ---- >>
      public Multiplication(Hardware H1, Hardware H2) : base() {
        this.H1 = H1;
        this.H2 = H2;
      }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      // << ---- O V E R R I D E S ---- >>
      public override cPose GetPose() {
        if (!UseH2) {
          var TH1Pose = H1.Pose;
          if (H1.PoseID != H1PoseID) {
            pose = TH1Pose;
            H1PoseID = H1.PoseID;
            PoseIDH1++;
          }
          PoseID = PoseIDH1;
        } else {
          var TH2Pose = H2.Pose;
          if (H2.PoseID != H2PoseID) {
            pose = TH2Pose;
            H2PoseID = H2.PoseID;
            PoseIDH2++;
          }
          PoseID = PoseIDH2;
        }
        return pose;
      }
      public override cPose GetNextPose() {
        if (!UseH2) {
          var h1nPose = H1.nPose;
          if (H1.nPoseID != H1nPoseID) {
            npose = h1nPose;
            H1nPoseID = H1.nPoseID;
            nPoseIDH1++;
          }
          nPoseID = nPoseIDH1;
        } else {
          var h2nPose = H2.nPose;
          if (H2.nPoseID != H2nPoseID) {
            npose = h2nPose;
            H2nPoseID = H2.nPoseID;
            nPoseIDH2++;
          }
          nPoseID = nPoseIDH2;
        }
        return npose;
      }
      public override cPose GetDeltaPose() {
        if (!UseH2) {
          var h1dPose = H1.dPose;
          if (H1.dPoseID != H1dPoseID) {
            dpose = h1dPose;
            H1dPoseID = H1.dPoseID;
            dPoseIDH1++;
          }
          dPoseID = dPoseIDH1;
        } else {
          var h2dPose = H2.dPose;
          if (H2.dPoseID != H2dPoseID) {
            dpose = h2dPose;
            H2dPoseID = H2.dPoseID;
            dPoseIDH2++;
          }
          dPoseID = dPoseIDH2;
        }
        return dpose;
      }
      public override void SetDeltaPose(cPose value, Hardware Reference) {
        if (ValidStep < GlobStep - 60) {
          var M1 = H1.Pose.Mat;
          var M2 = H1.Pose.Mat;
          if (Math.Abs(M1.M11 - M2.M11) > 0.01
            && Math.Abs(M1.M12 - M2.M12) > 0.01
            && Math.Abs(M1.M22 - M2.M22) > 0.01
            && Math.Abs(M1.M23 - M2.M23) > 0.01
            && Math.Abs(M1.M33 - M2.M33) > 0.01
            && Math.Abs(M1.M41 - M2.M41) > 0.01
            && Math.Abs(M1.M42 - M2.M42) > 0.01
            && Math.Abs(M1.M43 - M2.M43) > 0.01) {
            MyLog("<<Error>>: Multiplicated parts must have equivalent shapes.");
            ErrorFlag = GlobStep + 1000;
            Invalid = true;
          } else {
            Invalid = false;
          }
          ValidStep = GlobStep;
        }
        if (!Invalid) {
          H1.Move(value, Reference);
          UseH2 = true;
          H2.Move(value, Reference);
          UseH2 = false;
        } else {
          MyEcho("< < Error > >: Multiplicated parts must have equivalent shapes.");
        }
      }
      public override void GoHome(double Speed) {
        H1.GoHome(Speed);
        H2.GoHome(Speed);
      }
      public override void SetHome() {
        H1.SetHome();
        H2.SetHome();
      }
      public override void DebugInfo() {
        H1.DebugInfo();
        H2.DebugInfo();
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      Hardware H1;
      Hardware H2;
      bool UseH2 {
        get { return useH2; }
        set {
          useH2 = value;
          nPoseID = value ? dPoseIDH2 : dPoseIDH1;
        }
      }
      bool useH2, Invalid = false;
      cPose pose = new cPose(), npose = new cPose(), dpose = new cPose();
      uint H1PoseID, H2PoseID, PoseIDH1, PoseIDH2, H1nPoseID, H2nPoseID, nPoseIDH1, nPoseIDH2, H1dPoseID, H2dPoseID, dPoseIDH1, dPoseIDH2, ValidStep = 0;
    }
    /*
      <<  ----  S O L I D  --------------------------------------------------------------------  >>           S O L I D
      <<  --------------------------------------------------------------------  S O L I D  ----  >>           S O L I D
    V1.1*/
    class Solid : Hardware {

      // << ---- C O N S T R U C T O R ---- >>
      public Solid(
        double X
        , double Y
        , double Z) {
        solidPose = new cPose(new Vector3D(X, Y, Z));
        soliddPose = new cPose();
      }

      // << ---- O V E R R I D E S ---- >>
      public override cPose GetPose() => solidPose;
      public override cPose GetNextPose() => solidPose;
      public override cPose GetDeltaPose() => soliddPose;
      public override void SetDeltaPose(cPose TargetdPose, Hardware Reference) { }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      cPose solidPose, soliddPose;
    }

    /*
      <<  ----  R O T A R Y  --------------------------------------------------------------------  >>           R O T A R Y
      <<  ------------------------------------  R O T A R Y  ------------------------------------  >>           R O T A R Y
    V1.1*/
    class Rotary : Hardware {
      // << ---- C O N S T R U C T O R ---- >>
      public Rotary(
        String Axis = "Z"
        , double OriMode = 0
        , double MaxSpeed = 10
        , double Home = 0
        , bool AllowHome = true  // (rpm)
         ) : base() {  // (0-1)


        if (OriMode < 0 || OriMode > 1) {
          var OldOri = OriMode;
          OriMode = Math.Max(0, Math.Min(1, OriMode));

          MyLog("<<Warning>>: The OriMode parameter must be between 0 and 1. " + OldOri + " will be replaced by " + OriMode + ".");
          WarningFlag = GlobStep + 200;
        }

        this.Home = Home;
        this.Axis = Axis;
        this.MaxSpeed = MaxSpeed;
        this.OriMode = OriMode;
        DeadBand = 0.2;  // (sqrt of Distance)
        this.AllowHome = AllowHome;
      }
      // << ---- I N T E R F A C E ---- >>
      public virtual double GetAngle() => 0;
      public virtual double GetDeltaAngle() => 0;
      public virtual void SetDeltaAngle(double dAngle = 0) { }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public double DeadBand, MaxSpeed;
      public double OriMode;
      public String Axis;
      public bool AllowHome;
      public double Angle {
        get {
          if (AngleStep < GlobStep) {
            angle = GetAngle();
            AngleStep = GlobStep;
          }
          return angle;
        }
      }
      public double dAngle {
        get {
          dangle = GetDeltaAngle();
          return dangle;
        }
        set {
          if (Math.Abs(value) > 0.02 * dt) {
            value = ((56 * value) + Buffer) / 57;
            var S = MaxSpeed * dt;
            SetDeltaAngle(Clamp(value, -S, S));
            SleepMode = false;
          } else {
            SetDeltaAngle(0);
          }
          Buffer = value;
        }
      }
      // << ---- O V E R R I D E S ---- >>
      override public double Home {
        get { return home; }
        set { home = value * Math.PI / 180; }
      }
      public override cPose GetPose() {
        var TAngle = Angle;
        if (TAngle != LastAngle) {
          pose.Mat = RotateOnAxis(Axis, TAngle);
          PoseID++;
          LastAngle = TAngle;
        }
        return pose;
      }
      public override cPose GetNextPose() {
        var TnAngle = Angle + dAngle;
        if (TnAngle != LastnAngle) {
          npose.Mat = RotateOnAxis(Axis, TnAngle);
          nPoseID++;
          LastnAngle = TnAngle;
        }
        return npose;
      }
      public override cPose GetDeltaPose() {
        var TdAngle = dAngle;
        if (TdAngle != LastdAngle) {
          dpose.Mat = RotateOnAxis(Axis, TdAngle);
          dPoseID++;
          LastdAngle = TdAngle;
        }
        return dpose;
      }
      public override void SetDeltaPose(cPose TargetdPose, Hardware Reference) {  // (dPose/dt)
        Vector2D PlanePos = RelativePlane(Reference.Pose.Pos, Axis);
        var Temp = 0.0;
        var Temp2 = 0.0;
        if (OriMode != 0) {
          var Ref = Reference.dPose;
          var RA = RelOri(Reference.dPose, Axis);
          var A = RelOri(TargetdPose, Axis);
          Temp = (Buffer2 + ((A - RA - dAngle) * 10)) / 11;
          Buffer2 = Temp;
        }

        dAngle = 0;
        if (OriMode != 1) {
          if ((PlanePos.X * PlanePos.X) + (PlanePos.Y * PlanePos.Y) > DeadBand) {
            Vector2D PTV = RelativePlane(TargetdPose.Pos, Axis);
            Vector2D ENPP = RelativePlane(Reference.nPose.Pos, Axis);
            Vector2D TNPP = (PlanePos + PTV);
            double ENA = Math.Atan2(ENPP.Y, ENPP.X);
            double TNA = Math.Atan2(TNPP.Y, TNPP.X);
            Temp2 = AngleProxy(ENA, TNA);
          }
        }
        dAngle = ((1 - OriMode) * Temp2) + ((OriMode) * Temp);

      }
      public override void GoHome(double Speed) {
        if (Home != Angle && AllowHome) {
          var ang = -Clamp(AngleProxy(Angle, Home) * Speed, -Speed * dt, Speed * dt);
          dAngle = ang * dt;
        }
      }
      public override void SetHome() => Home = Angle;
      public override void DebugInfo() {
        var Text = "Rotary: " + Axis + " " + (AngleProxy(0, Angle) / Math.PI * 180).ToString("0") + "°";
        if (dAngle > 0) {
          Text += "+" + (dAngle / dt / Math.PI * 30).ToString("0") + "rpm";
        } else {
          Text += (dAngle / dt / Math.PI * 30).ToString("0") + "rpm";
        }
        MyEcho(Text);
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      cPose pose = new cPose(), npose = new cPose(), dpose = new cPose();
      double home = 0, angle = 0, dangle = 0, LastAngle = -100000, LastnAngle = -100000, LastdAngle = -100000, Buffer = 0, Buffer2 = 0;
      uint AngleStep = 0;
      // << ---- P R I V A T E   F U N C T I O N S ---- >>
      MatrixD RotateOnAxis(String Axis, double Angle = 0) {
        switch (Axis) {
          case "X": return RotX(Angle);
          case "Y": return RotY(Angle);
          case "Z": return RotZ(Angle);
          case "-X": return RotX(-Angle);
          case "-Y": return RotY(-Angle);
          case "-Z": return RotZ(-Angle);
          default: return MatrixD.Identity;
        }
      }
      Vector2D RelativePlane(Vector3D Vec, String Axis) {
        switch (Axis) {
          case "X": return new Vector2D(Vec.Y, -Vec.Z);
          case "Y": return new Vector2D(-Vec.X, -Vec.Z);
          case "Z": return new Vector2D(-Vec.X, Vec.Y);
          case "-X": return new Vector2D(-Vec.Y, -Vec.Z);
          case "-Y": return new Vector2D(Vec.X, -Vec.Z);
          case "-Z": return new Vector2D(-Vec.Y, Vec.X);
          default: return new Vector2D(Vec.Y, Vec.Z);
        }
      }
      public double RelOri(cPose Ori, String Axis) {
        double Out;
        var Mat = Ori.Mat;
        MyDebug(Axis);
        switch (Axis) {
          case "X": Out = Math.Atan2(Math.Round(Mat.M32 - Mat.M23, 12), Math.Round(Mat.M22 + Mat.M33, 12)); break;
          case "Y": Out = Math.Atan2(Math.Round(Mat.M13 - Mat.M31, 12), Math.Round(Mat.M11 + Mat.M33, 12)); break;
          case "Z": Out = Math.Atan2(Math.Round(Mat.M21 - Mat.M12, 12), Math.Round(Mat.M11 + Mat.M22, 12)); break;
          case "-X": Out = -Math.Atan2(Math.Round(Mat.M32 - Mat.M23, 12), Math.Round(Mat.M22 + Mat.M33, 12)); break;
          case "-Y": Out = -Math.Atan2(Math.Round(Mat.M13 - Mat.M31, 12), Math.Round(Mat.M11 + Mat.M33, 12)); break;
          case "-Z": Out = -Math.Atan2(Math.Round(Mat.M21 - Mat.M12, 12), Math.Round(Mat.M11 + Mat.M22, 12)); break;
          default: Out = 0; break;
        }
        return Out;
      }
      public double AngleProxy(double A1 = 0, double A2 = 0) {  // Give the smallest difference between two angles in rad
        A1 = A2 - A1;
        A1 = Mod(A1 + Math.PI, (double)2 * Math.PI) - Math.PI;
        return A1;
      }
      public double Mod(double A, double N) => A - (Math.Floor(A / N) * N);
    }
    /*
      <<  ----  L I N E A R  --------------------------------------------------------------------  >>           L I N E A R
      <<  --------------------------------------------------------------------  L I N E A R  ----  >>           L I N E A R
    V1.1*/
    class Linear : Hardware {

      // << ---- C O N S T R U C T O R ---- >>
      public Linear(
        String Axis = "X"
        , double MaxSpeed = 5
        , double Home = 0
        , bool AllowHome = true) {  // (m/s)
        Direction = AxisToDirection(Axis);
        this.MaxSpeed = MaxSpeed;
        this.Axis = Axis;
        this.Home = Home;
        this.AllowHome = AllowHome;
      }
      // << ---- I N T E R F A C E ---- >>
      public virtual double GetLength() => 0;
      public virtual double GetDeltaLength() => 0;
      public virtual void SetDeltaLength(double dLength = 0) { }

      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public double MaxSpeed;
      public String Axis;
      public bool AllowHome;
      public double Length {
        get {
          if (LengthStep < GlobStep) {
            length = GetLength();
            LengthStep = GlobStep;
          }
          return length;
        }
      }
      public double dLength {
        get {
          dlength = GetDeltaLength();
          return dlength;
        }
        set {
          if (Math.Abs(value) > 0.001 * dt) {
            var S = MaxSpeed * dt;
            SetDeltaLength(Clamp(((6 * value) + Buffer) / 7, -S, S));
            SleepMode = false;
          } else {
            SetDeltaLength(Buffer / 2);
          }
          Buffer = value;
        }
      }
      // << ---- O V E R R I D E S ---- >>
      public override cPose GetPose() {
        var TLength = Length;
        if (TLength != LastLength) {
          pose.Mat = MatrixD.CreateTranslation(Direction * TLength);
          LastLength = TLength;
          PoseID++;
        }
        return pose;
      }
      public override cPose GetNextPose() {
        var TnLength = Length + dLength;
        if (TnLength != LastnLength) {
          npose.Mat = MatrixD.CreateTranslation(Direction * (TnLength));
          nPoseID++;
          LastnLength = TnLength;
        }
        return npose;
      }
      public override cPose GetDeltaPose() {
        var TdLength = dLength;
        if (TdLength != LastdLength) {
          dpose.Mat = MatrixD.CreateTranslation(Direction * TdLength);
          dPoseID++;
          LastdLength = TdLength;
        }
        return dpose;
      }
      public override void SetDeltaPose(cPose TargetdPose, Hardware Reference) => dLength = ((double)Vector3D.Dot(TargetdPose.Pos - Reference.dPose.Pos, Direction));
      public override void GoHome(double Speed) {
        if (Home != Length && AllowHome)
          dLength = Clamp((Home - Length) * Speed * dt, -Speed * dt, Speed * dt);
      }
      public override void SetHome() => Home = Length;
      public override void DebugInfo() {
        var Text = "Linear: " + Axis + " " + Length.ToString("0.0") + "m";
        if (dLength > 0) {
          Text += "+" + (dLength / dt).ToString("0.0") + "m/s";
        } else {
          Text += (dLength / dt).ToString("0.0") + "m/s";
        }
        MyEcho(Text);
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      Vector3D Direction;
      cPose pose = new cPose(), npose = new cPose(), dpose = new cPose();
      double length = 0, dlength = 0, LastLength = -100000, LastdLength = -100000, LastnLength = -100000, Buffer = 0;
      uint LengthStep = 0;
      // << ---- P R I V A T E   F U N C T I O N S ---- >>
      Vector3D AxisToDirection(String Axis) {
        switch (Axis) {
          case "X": return new Vector3D(1, 0, 0);
          case "Y": return new Vector3D(0, 1, 0);
          case "Z": return new Vector3D(0, 0, 1);
          case "-X": return new Vector3D(-1, 0, 0);
          case "-Y": return new Vector3D(0, -1, 0);
          case "-Z": return new Vector3D(0, 0, -1);
          default: return new Vector3D(0, 0, 1);
        }
      }
    }
    /*
      <<  ----  R O T O R  --------------------------------------------------------------------  >>           R O T O R
      <<  --------------------------------------------------------------------  R O T O R  ----  >>           R O T O R
    V1.1*/
    class Rotor : Rotary {

      // << ---- C O N S T R U C T O R ---- >>
      public Rotor(
        String Name
        , String Axis = "Z"
        , double OriMode = 0
        , double MaxSpeed = 60  // (rpm)

        , bool Override = false
        , double Offset = 0  // (deg)
        , double SoftMaxLimit = 1
        , double SoftMinLimit = 1
        , double Home = 0
        , bool AllowHome = true)
        : base(Axis, OriMode, MaxSpeed, Home, AllowHome) {

        MyLog("Loading Rotor: " + Name);
        this.Offset = Offset * Math.PI / 180;
        this.Name = Name;
        this.SoftMinLimit = SoftMinLimit;
        this.SoftMaxLimit = SoftMaxLimit;
        this.Override = Override;
        if (!HardwareList.ContainsKey(Name)) HardwareList.Add(Name, this);
      }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public String Name;
      public double SoftMinLimit;
      public double SoftMaxLimit;
      public double Offset;
      public IMyMotorStator Motor {
        get {
          if (RotorStep < GlobStep - NORMALFPS) {
            motor = (IMyMotorStator)MyGTS.GetBlockWithName(Name);
            RotorStep = GlobStep;
            if (motor == null) {
              MyLog("<<Error>>: Rotor '" + Name + "' Not found.");
              ErrorFlag = GlobStep + 200;
            }
          }
          return motor;
        }
      }
      // << ---- O V E R R I D E S ---- >>
      public override void SetDeltaAngle(double dAngle = 0) {
        double Vel;
        if (!Override && Motor != null) {
          // Get the desired acceleration
          Vel = dAngle / Math.PI * 30 / dt;
          // Softening the limits by limiting speed near them
          double SoftMax = (Motor.UpperLimitRad < 360 ?
          Math.Min(1, Math.Abs(Motor.UpperLimitRad - Motor.Angle) / SoftMaxLimit) : 1);
          double SoftMin = (Motor.LowerLimitRad > -360 ?
          Math.Min(1, Math.Abs(Motor.LowerLimitRad - Motor.Angle) / SoftMinLimit) : 1);
          Vel = Math.Min(MaxSpeed * SoftMax, Vel);
          Vel = Math.Max(-MaxSpeed * SoftMin, Vel);
          // Applying the velocity to the Motor
          if (Math.Abs(Vel) < MaxSpeed + 5) {
            //Motor.SetValue("Velocity",(Single)Vel );  // (rpm)  // old ways
            Motor.TargetVelocityRad = (Single)(Vel * Math.PI / 30);  // (rad)
          }
        }
      }
      public override double GetAngle() {
        if (Motor != null) {
          return -Motor.Angle + Offset;  // (rad)
        } else {
          return 0;
        }
      }
      public override double GetDeltaAngle() {
        if (Motor != null
            && Motor.Enabled && Motor.Angle < Motor.UpperLimitRad
            && Motor.Angle > Motor.LowerLimitRad) {
          return -Motor.TargetVelocityRad * dt; // from rad to drad/ds
        } else {
          return 0;
        }
      }
      public override void GoHome(double Speed) {
        if (Home != Angle && AllowHome) {
          var ang = -Clamp(AngleProxy(Angle, Home) * Speed, -Speed * dt, Speed * dt) * dt;
          // Check if it would make us cross a limit
          // COPY PASTE THIS STUFF
          if (ang > 0 && motor.UpperLimitRad < 7) {
            var targetHome = Home;
            while (targetHome < Angle) {
              targetHome += 2 * Math.PI;
            }
            if (targetHome > motor.UpperLimitRad + Offset + 0.001) {
              ang *= -1;
            }
          } else if (ang < 0 && motor.LowerLimitRad > -7) {
            var targetHome = Home;
            while (targetHome > Angle) {
              targetHome -= 2 * Math.PI;
            }
            if (targetHome < motor.LowerLimitRad + Offset - 0.001) {
              ang *= -1;
            }
          }
          dAngle = ang;
          // END COPY PASTE THIS STUFF
        }
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      IMyMotorStator motor;
      uint RotorStep = 0;

    }
    /*
      <<  ----  H Y D R A U L I C  --------------------------------------------------------------------  >>           H Y D R A U L I C
      <<  --------------------------------------------------------------------  H Y D R A U L I C  ----  >>           H Y D R A U L I C
    V1.1*/
    class Hydraulic : Rotary {
      // << ---- C O N S T R U C T O R ---- >>
      public Hydraulic(
        String Axis = "Z"
        , double OriMode = 0
        , double MaxSpeed = 60  // (rpm)
        , Hardware Actuator = null
        , bool Invert = false
        , double Tangent1 = 1
        , double Normal1 = 0
        , double Tangent2 = 1
        , double Normal2 = 0
        , double Home = 0
        , bool AllowHome = true)
        : base(Axis, OriMode, MaxSpeed, Home, AllowHome) {

        this.Actuator = Actuator;
        Sign = Invert ? -1 : 1;
        var Length1 = Math.Sqrt((Tangent1 * Tangent1) + (Normal1 * Normal1));
        var Length2 = Math.Sqrt((Tangent2 * Tangent2) + (Normal2 * Normal2));
        Offset = (-Math.Atan2(Normal1, Tangent1) - Math.Atan2(Normal2, Tangent2));
        MyLog("Offset= " + (Offset / Math.PI * 180).ToString());
        L1_2p2_2 = (Length1 * Length1) + (Length2 * Length2);
        L1mL2m2 = 2 * Length1 * Length2;
      }
      public Hardware Actuator;

      // << ---- O V E R R I D E S ---- >>
      public override double GetAngle() {
        var D = Actuator.Pose.Pos.X;
        var A = Math.Acos(((D * D) - L1_2p2_2) / L1mL2m2) + Offset;
        MyDebug("Hydraulic Angle: " + (A / Math.PI * 180).ToString("0.00") + "°");
        return A;
      }
      public override double GetDeltaAngle() {
        var D = Actuator.Pose.Pos.X;
        var nD = Actuator.nPose.Pos.X;
        if (D != LD || nD != LnD) {
          nangle = Math.Acos(((nD * nD) - L1_2p2_2) / L1mL2m2) - Math.Acos(((D * D) - L1_2p2_2) / L1mL2m2);
          LD = D;
          LnD = nD;
        }
        return nangle;
      }
      public override void SetDeltaAngle(double dAngle) {
        double D = Actuator.Pose.Pos.X;
        double nD = Math.Sqrt(L1_2p2_2 + (L1mL2m2 * Math.Cos(Angle + dAngle - Offset)));
        Actuator.Move(new cPose(new Vector3D(-(nD - D) * Sign, 0, 0)), Actuator);
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      double nangle = 0;
      double LD = 0;
      double LnD = 0;
      double L1_2p2_2 = 0;
      double L1mL2m2 = 0;
      int Sign = 0;
      double Offset = 0;

    }
    /*
      <<  ----  P I S T O N  --------------------------------------------------------------------  >>           P I S T O N
      <<  --------------------------------------------------------------------  P I S T O N  ----  >>           P I S T O N
    V1.1*/
    class Piston : Linear {

      // << ---- C O N S T R U C T O R ---- >>
      public Piston(
        String Name
        , String Axis = "X"
        , bool Override = false
        , double MaxSpeed = 5  // (m/s)
        , double SoftMaxLimit = 1
        , double SoftMinLimit = 1
        , double Home = 0
        , bool AllowHome = true)
        : base(Axis, MaxSpeed, Home, AllowHome) {

        MyLog("Loading Piston: " + Name);
        this.Name = Name;
        this.SoftMinLimit = SoftMinLimit;
        this.SoftMaxLimit = SoftMaxLimit;
        this.Override = Override;
        if (!HardwareList.ContainsKey(Name)) HardwareList.Add(Name, this);
      }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public String Name;
      public double SoftMinLimit;
      public double SoftMaxLimit;
      public IMyPistonBase MyPiston {
        get {
          if (PistonStep < GlobStep - NORMALFPS) {
            piston = (IMyPistonBase)MyGTS.GetBlockWithName(Name);
            PistonStep = GlobStep;
          }
          if (piston == null) {
            MyLog("<<Error>>: Piston '" + Name + "' Not found.");
            ErrorFlag = GlobStep + 200;
          }
          return piston;
        }
        private set {
          piston = value;
        }
      }
      // << ---- O V E R R I D E S ---- >>
      public override double GetLength() {
        if (MyPiston != null) {
          return MyPiston.CurrentPosition;  // (m)
        } else {
          return 0;
        }
      }
      public override void SetDeltaLength(double dLength) {  // (dm/dt)
        double Vel = 0;
        if (!Override && MyPiston != null) {
          // Get the desired velocity
          Vel = dLength / dt;  // (m/s)
                               // Softening the limits by limiting speed near them
          double SoftMax = Math.Min(1, Math.Abs(MyPiston.MaxLimit - GetLength()) / SoftMaxLimit);
          double SoftMin = Math.Min(1, Math.Abs(MyPiston.MinLimit - GetLength()) / SoftMinLimit);
          Vel = Math.Min(MaxSpeed * SoftMax, Vel);
          Vel = Math.Max(-MaxSpeed * SoftMin, Vel);
          // Applying the velocity to the Piston
          if (Math.Abs(Vel) < 10)  // Ensure to not send impossible values
                                   // MyPiston.SetValue("Velocity", (Single)Vel ); // old ways
            MyPiston.Velocity = (Single)Vel;
        }
      }
      public override double GetDeltaLength() {
        if (MyPiston != null
          && MyPiston.Enabled
          && MyPiston.CurrentPosition <= MyPiston.MaxLimit - 0.01
          && MyPiston.CurrentPosition >= MyPiston.MinLimit + 0.01) {
          return MyPiston.Velocity * dt;  // (dm/dt)
        } else {
          return 0;  // (dm/dt)
        }
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      IMyPistonBase piston;
      uint PistonStep;
    }
    /*
      <<  ----  R O T O R W H E E L  --------------------------------------------------------------------  >>           R O T O R W H E E L
      <<  --------------------------------------------------------------------  R O T O R W H E E L  ----  >>           R O T O R W H E E L
    V1.1*/
    class RotorWheel : Linear {

      // << ---- C O N S T R U C T O R ---- >>
      public RotorWheel(
          String Axis = "Y"
        , double Radius = 3  // (m)
        , Rotary Wheel = null
        , int Direction = 1
        , double MaxSpeed = 20
        , bool AllowHome = false) // (m/s)
        : base(Axis, MaxSpeed, 0, AllowHome) {
        bool Error = false;
        if (Wheel == null) {
          MyLog("<<Error>>: Wheel undefined.");
          ErrorFlag = GlobStep + 200;
          Error = true;
        }
        if (Wheel.Axis == Axis) {
          MyLog("<<Error>>: A RotorWheel's axis of rotation can't be the same as its axis of displacement.");
          ErrorFlag = GlobStep + 200;
          Error = true;
        }
        if (!Error) {
          this.Wheel = Wheel;
          this.Radius = Radius;
          this.Direction = Direction;
          Position = 0;
        }
      }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public Rotary Wheel;
      public double Radius = 0.75;
      public int Direction = 1;
      // << ---- O V E R R I D E S ---- >>
      public override double GetLength() {
        Position += GetDeltaLength() * Direction / 6;
        return Position;
      }
      public override void SetDeltaLength(double dLength) {  // (dm/dt)
        if (Radius != 0)
          Wheel.dAngle = -dLength / Radius * Direction * 0.999;
      }
      public override double GetDeltaLength() => Wheel.dAngle * Radius * Direction;  // (dm/dt)
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      double Position;
    }
    /*
      <<  ----  S O L I D   L G  --------------------------------------------------------------------  >>           S O L I D   L G
      <<  --------------------------------------------------------------------  S O L I D   L G  ----  >>           S O L I D   L G
    V1.1*/
    class SolidLG : Solid {

      // << ---- C O N S T R U C T O R ---- >>
      public SolidLG(
        double X = 0
        , double Y = 0
        , double Z = 0)
        : base(X * LG, Y * LG, Z * LG) {
        MyLog("Loading SolidLG: (" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")");
      }
    }
    /*
      <<  ----  S O L I D   S G  --------------------------------------------------------------------  >>           S O L I D   S G
      <<  --------------------------------------------------------------------  S O L I D   S G  ----  >>           S O L I D   S G
    V1.1*/
    class SolidSG : Solid {

      // << ---- C O N S T R U C T O R ---- >>
      public SolidSG(
        double X = 0
        , double Y = 0
        , double Z = 0)
        : base(X * SG, Y * SG, Z * SG) {
        MyLog("Loading SolidSG: (" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")");
      }
    }
    /*
      <<  ----  C U B I T S  --------------------------------------------------------------------  >>           C U B I T S
      <<  --------------------------------------------------------------------  C U B I T S  ----  >>           C U B I T S
    V1.1*/
    class Cubits : Solid {

      // << ---- C O N S T R U C T O R ---- >>
      public Cubits(
        double X = 0
        , double Y = 0
        , double Z = 0)
        : base(X * C, Y * C, Z * C) {
        MyLog("Loading Cubits: (" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")");
      }
    }
    /*
      <<  ----  c P O S E  --------------------------------------------------------------------  >>           c P O S E
      <<  --------------------------------------------------------------------  c P O S E  ----  >>           c P O S E
    V1.1*/
    class cPose {

      // << ---- C O N S T R U C T O R ---- >>
      public cPose(MatrixD Mat) {
        this.Mat = Mat;

        ori = MatrixD.Identity;
        NullOri = (Mat.M11 == 1 && Mat.M22 == 1 && Mat.M33 == 1);
      }
      public cPose(Vector3D Pos = new Vector3D()) {
        Mat = MatrixD.CreateTranslation(Pos);
        NullOri = true;
        ori = MatrixD.Identity;
      }
      public cPose(cPose O) {
        Mat = new MatrixD(
          O.Mat.M11, O.Mat.M12, O.Mat.M13, O.Mat.M14,
          O.Mat.M21, O.Mat.M22, O.Mat.M23, O.Mat.M24,
          O.Mat.M31, O.Mat.M32, O.Mat.M33, O.Mat.M34,
          O.Mat.M41, O.Mat.M42, O.Mat.M43, O.Mat.M44);
        ori = MatrixD.Identity;
        NullOri = (Mat.M11 == 1 && Mat.M22 == 1 && Mat.M33 == 1);
      }

      // << ---- o p e r a t o r s ---- >>
      static public cPose operator +(cPose c1, cPose c2) {
        if (c1.NullOri) {
          if (c2.NullOri) {
            return new cPose(c1.Pos + c2.Pos);
          } else {
            MatrixD M = c2.Mat;
            return new cPose(new MatrixD(
              M.M11, M.M12, M.M13, 0,
              M.M21, M.M22, M.M23, 0,
              M.M31, M.M32, M.M33, 0,
              M.M41 + c1.Pos.X, M.M42 + c1.Pos.Y, M.M43 + c1.Pos.Z, 1));
          }
        } else {
          return new cPose(c2.Mat * c1.Mat);  // <---- Check order (ok)
        }
      }
      static public cPose operator -(cPose c1, cPose c2) {
        if (c1.NullOri) {
          if (c2.NullOri) {
            return new cPose(c1.Pos - c2.Pos);
          } else {
            MatrixD M = MatrixD.Invert(c2.Mat);
            return new cPose(new MatrixD(
              M.M11, M.M12, M.M13, 0,
              M.M21, M.M22, M.M23, 0,
              M.M31, M.M32, M.M33, 0,
              M.M41 + c1.Pos.X, M.M42 + c1.Pos.Y, M.M43 + c1.Pos.Z, 1));
          }
        } else {
          return new cPose(MatrixD.Invert(c2.Mat) * c1.Mat);  // <---- Check order
        }
      }
      static public cPose operator -(cPose c1) {
        return new cPose(MatrixD.Invert(c1.Mat));
      }
      static public cPose operator *(cPose c1, double c2) {
        var outp = new cPose();
        outp.Blend(c1, c2);
        return outp;
      }
      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public MatrixD Mat {
        get { return mat; }
        set {
          if (mat != value) {
            mat = value;
            UTDOri = false;
            NullOri = (mat.M11 == 1 && mat.M22 == 1 && mat.M33 == 1);
            UTDYaw = false;
            UTDPitch = false;
            UTDRoll = false;
          }
        }
      }
      public MatrixD Ori {
        get {
          if (!UTDOri) {
            ori = MatrixD.Identity;
            ori.M11 = Mat.M11; ori.M12 = Mat.M12; ori.M13 = Mat.M13;
            ori.M21 = Mat.M21; ori.M22 = Mat.M22; ori.M23 = Mat.M23;
            ori.M31 = Mat.M31; ori.M32 = Mat.M32; ori.M33 = Mat.M33;
            UTDOri = true;
          }
          return ori;
        }
        set {
          var Mat = this.Mat;
          mat.M11 = value.M11; mat.M12 = value.M12; mat.M13 = value.M13;
          mat.M21 = value.M21; mat.M22 = value.M22; mat.M23 = value.M23;
          mat.M31 = value.M31; mat.M32 = value.M32; mat.M33 = value.M33;
          UTDOri = false;
          UTDYaw = false;
          UTDPitch = false;
          UTDRoll = false;
          NullOri = (mat.M11 == 1 && mat.M22 == 1 && mat.M33 == 1);
        }
      }
      public Vector3D Pos {
        get {
          return mat.Translation;
        }
        set {
          mat.M41 = value.X;
          mat.M42 = value.Y;
          mat.M43 = value.Z;
        }
      }
      public double Yaw {
        get {
          if (!UTDYaw) { yaw = -Math.Atan2(-mat.M12, mat.M11); UTDYaw = true; }
          return yaw;
        }
      }
      public double Pitch {
        get {
          if (!UTDPitch) { pitch = -Math.Asin(mat.M13); UTDPitch = true; }
          return pitch;
        }
      }
      public double Roll {
        get {
          if (!UTDRoll) { roll = -Math.Atan2(-mat.M23, mat.M33); UTDRoll = true; }
          return roll;
        }
      }
      // << ---- P U B L I C   F U N C T I O N S ---- >>
      public void Blend(cPose c1, double M) =>
        // this.Mat = this.Mat*(1-M)+c1.Mat*M;
        Mat = MatrixD.Lerp(Mat, c1.Mat, M);
      // << ---- O V E R R I D E S ---- >>
      public override String ToString() {
        String Text = "Pos: ";
        Text += Pos.X.ToString("0.000") + ", ";
        Text += Pos.Y.ToString("0.000") + ", ";
        Text += Pos.Z.ToString("0.000") + "\nOri: ";
        Text += mat.M11.ToString("0.0000") + ", ";
        Text += mat.M12.ToString("0.0000") + ", ";
        Text += mat.M23.ToString("0.0000") + "\n     ";
        Text += mat.M21.ToString("0.0000") + ", ";
        Text += mat.M22.ToString("0.0000") + ", ";
        Text += mat.M23.ToString("0.0000") + "\n     ";
        Text += mat.M31.ToString("0.0000") + ", ";
        Text += mat.M32.ToString("0.0000") + ", ";
        Text += mat.M33.ToString("0.0000");
        return Text;
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      MatrixD mat;
      MatrixD ori;
      bool UTDOri = false;
      bool NullOri = false;
      double yaw = 0;
      double pitch = 0;
      double roll = 0;
      bool UTDYaw = false;
      bool UTDPitch = false;
      bool UTDRoll = false;
      // << ---- P R I V A T E   F U N C T I O N S ---- >>
    }
    /*
      <<  ----  L I M I T S  --------------------------------------------------------------------  >>           L I M I T S
      <<  --------------------------------------------------------------------  L I M I T S  ----  >>           L I M I T S
    V1.0*/
    class Limits {
      // << ---- C O N S T R U C T O R ---- >>
      public Limits(bool Inside = true) {
        this.Inside = Inside;
      }
      // << ---- I N T E R F A C E ---- >>
      public virtual Vector3D CheckLimits(Vector3D Pos) => new Vector3D();

      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      bool Inside;
    }
    /*
      <<  ----  C O N T R O L L E R  --------------------------------------------------------------------  >>           C O N T R O L L E R
      <<  --------------------------------------------------------------------  C O N T R O L L E R  ----  >>           C O N T R O L L E R
    V1.1*/
    class Controller {

      // << ---- C O N S T R U C T O R ---- >>
      public Controller(
        Hardware Arm = null
        , String Name = ""
        , Limits Workzone = null
        , double Speed = 5
        , double Softness = 5
        ) {

        MyLog("Loading Controller of name: " + Name);
        this.Arm = Arm;
        this.Name = Name;
        this.Speed = Speed;
        this.Workzone = Workzone;
        this.Softness = Softness;
        MyControllers.Add(this);
        if (Arm == null) {
          MyLog("No Arm selected. DefaultArm will be used instead.");
          this.Arm = DefaultArm;
        }

        VirtualPose = this.Arm.Pose;
      }

      // << ---- I N T E R F A C E ---- >>
      public virtual cPose GetInputs() => new cPose();   // (m/s, rad/s)
      public virtual void ExecuteCommand(String Command) { }

      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public Hardware Arm;
      public String Name;
      public double Softness;
      public double Speed;
      public double HomeSpeed = 1;
      public bool GoingHome = false;
      public static List<Controller> MyControllers = new List<Controller>();

      // << ---- P U B L I C   F U N C T I O N ---- >>
      static public void UpdateControllers(String Argument = "") {
        foreach (Controller Contr in MyControllers) {
          Contr.Update(Argument);
        }
      }
      public void GoHome(double HomeSpeed = 1) {
        MyLog("---- Going home ----");
        GoingHome = true;
        this.HomeSpeed = HomeSpeed;
        Correction = new cPose();
      }
      public void SetHome() => Arm.SetHome();
      public void Update(String Argument = "") {
        ParseArgument(Argument);
        cPose InputMovement;
        InputMovement = /*ConsiderWorkzone( */GetInputs();// );  // (dm/dt, drad/dt)
        if (GoingHome) {
          if (InputMovement.Pos.Length() < 0.001 && InputMovement.Mat.M11 > 0.999 && InputMovement.Mat.M22 > 0.999 && InputMovement.Mat.M33 > 0.999) {
            MyEcho(Name + " Going Home...");
            Arm.GoHome(HomeSpeed);
          } else {
            MyLog("'GoHome' interrupted by mouse or keyboard inputs");
            GoingHome = false;
          }
        } else {
          ControlMovement.Blend(InputMovement, 1 / Softness);
          ApplyMovement(ControlMovement);
          MyEcho("Current Pose:");
          MyEcho(Arm.Pose.Pos.ToString("0.0"));
          MyEcho("Current Input:");
          MyEcho(InputMovement.Pos.ToString("0.0"));
        }
        Arm.DebugInfo();
      }

      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      Limits Workzone;
      cPose VirtualPose, Correction = new cPose(), ControlMovement = new cPose();
      // << ---- P R I V A T E   F U N C T I O N S ---- >>
      cPose ConsiderWorkzone(cPose InputMovement) {  // (dm/dt, drad/dt)
        if (Workzone != null) {
          Vector3D ColisionVector = Workzone.CheckLimits(Arm.Pose.Pos);
          return InputMovement;  // TODO
        } else {
          return InputMovement;
        }
      }
      void ApplyMovement(cPose ControlMovement) {  // (dm/dt, drad/s)
        var ControlMovement2 = new cPose(ControlMovement);
        ControlMovement2 += Correction;
        Arm.Move(ControlMovement2, Arm);  // (dm/dt, drad/dt)
        Correction = (Correction * 0.9) + ((-Arm.dPose + ControlMovement) * 0.5);
      }
      void ParseArgument(String Argument = "") {
        if (Argument != "") {
          MyLog("Executing argument: " + Argument);
          String[] Lines = Argument.Split('\n');
          foreach (String Line in Lines) {
            String[] Commands = Line.Split(';');
            foreach (String Command in Commands) {
              String[] Special = Command.Split(' ');
              if (Name != "" && Special[0] == Name) {
                ExecuteCommand(Reform(Special, 1));
              } else {
                ExecuteCommand(Reform(Special, 0));
              }
            }
          }
        }
      }
      // Function used to reassemble the end of a string[] to get a double string
      public static String Reform(String[] Words, int i) {
        String Word = "";
        do {
          Word += Words[i];
          i++;
        } while (i < Words.Length && (Word += " ") != null);
        return Word;
      }
      // Function used to interpret an On/Off/Toggle
      public static bool OnOffToggle(String Word, bool Value = false) {
        if (Word == "On") {
          return true;
        } else if (Word == "Off" || Word == "0") {
          return false;
        } else if (Word == "Toggle" || Word == "-1") {
          return !Value;
        } else {
          MyLog("<<Error>>: " + Word + " is Invalid. Use 'On/1, Off/0 or Toggle/-1'");
          ErrorFlag = GlobStep + 200;
        }

        return Value;
      }
    }

    /*
      <<  ----  U S E R C O N T R O L  --------------------------------------------------------------------  >>           U S E R C O N T R O L
      <<  --------------------------------------------------------------------  U S E R C O N T R O L  ----  >>           U S E R C O N T R O L
    V1.0*/
    class UserControl : Controller {

      // << ---- C O N S T R U C T O R ---- >>
      public UserControl(
        Hardware Arm = null
        , String Name = ""
        , Limits Workzone = null
        , double Speed = 2
        , double Softness = 10
        , bool StartOn = true
        , String ShipControllerKeyword = "Arm Controller"
        , bool ReadKeyboard = true
        , bool ReadMouse = true
        , Hardware ReferenceFrame = null
        , double YawSpeed = 1
        , double PitchSpeed = 1
        , double RollSpeed = 1
        , bool UseArmAsReference = false
        ) : base(Arm, Name, Workzone, Speed, Softness) {
        OnOff = StartOn;
        this.ShipControllerKeyword = ShipControllerKeyword;
        this.ReadKeyboard = ReadKeyboard;
        this.ReadMouse = ReadMouse;
        this.ReferenceFrame = null;
        if (ReferenceFrame != null) {
          this.ReferenceFrame = ReferenceFrame;
        }
        if (UseArmAsReference) {
          MyLog("ReferenceFrame set to Arm");
          this.ReferenceFrame = this.Arm;
        }
        this.YawSpeed = YawSpeed;
        this.PitchSpeed = PitchSpeed;
        this.RollSpeed = RollSpeed;
        ContrStep = 0;
        UseTarget = false;
        ConstantMovement = new cPose();
      }

      // << ---- P U B L I C   V A R I A B L E S ---- >>
      public bool OnOff;
      public double YawSpeed;
      public double PitchSpeed;
      public double RollSpeed;
      public bool ReadKeyboard;
      public bool ReadMouse;
      public String ShipControllerKeyword;
      public Hardware ReferenceFrame;
      public cPose ConstantMovement;
      public cPose Target {
        get {
          return target;
        }
        set {
          UseTarget = (value.Mat != MatrixD.Identity);
          target = value;
          ConstantMovement = new cPose();
        }
      }

      // << ---- P U B L I C   F U N C T I O N ---- >>
      public void Move(double X = 0,
        double Y = 0,
        double Z = 0,
        double Yaw = 0,
        double Pitch = 0,
        double Roll = 0) {
        ConstantMovement = new cPose(new Vector3D(X, Y, Z) * dt) + new cPose(RotZ(Yaw * dt * Math.PI / 180));
        ConstantMovement += new cPose(RotY(Pitch * dt * Math.PI / 180)) + new cPose(RotX(Roll * dt * Math.PI / 180));
      }
      public void RelMove(double X = 0,
        double Y = 0,
        double Z = 0,
        double Yaw = 0,
        double Pitch = 0,
        double Roll = 0) {
        ConstantMovement = new cPose(new Vector3D(X, Y, Z) * dt) + new cPose(RotZ(Yaw * dt * Math.PI / 180));
        ConstantMovement += new cPose(RotY(Pitch * dt * Math.PI / 180)) + new cPose(RotX(Roll * dt * Math.PI / 180));
        ConstantMovement = ApplyReferenceFrame(ConstantMovement);
      }
      public void MoveTo(
        double X = 0,
        double Y = 0,
        double Z = 0,
        double Yaw = 0,
        double Pitch = 0,
        double Roll = 0) {
        MyLog("Moving to: " + X.ToString("0.00") + " " + Y.ToString("0.00") + " " + Z.ToString("0.00"));
        Target = new cPose(new Vector3D(X, Y, Z)) + new cPose(RotZ(Yaw * Math.PI / 180));
        Target += new cPose(RotY(Pitch * Math.PI / 180)) + new cPose(RotX(Roll * Math.PI / 180));
      }
      public void PrintPosition(String PanelName = "") {

        var Text = Name + "MoveTo ";
        Text += Arm.Pose.Pos.X.ToString("0.000") + " ";
        Text += Arm.Pose.Pos.Y.ToString("0.000") + " ";
        Text += Arm.Pose.Pos.Z.ToString("0.000") + " ";
        Text += (Arm.Pose.Yaw / Math.PI * 180).ToString("0.000") + " ";
        Text += (Arm.Pose.Pitch / Math.PI * 180).ToString("0.000") + " ";
        Text += (Arm.Pose.Roll / Math.PI * 180).ToString("0.000");

        if (PanelName == "") {
          MyLog("<<Warning>> " + PanelName + " not found. Printing in log instead.");
          WarningFlag = GlobStep + 500;
          MyLog(Text);
        } else {
          IMyTextPanel Panel = (IMyTextPanel)MyGTS.GetBlockWithName(PanelName);
          MyLog("Position Printed into " + PanelName);
          Panel.WriteText(Text, false);
        }
      }
      public void ClearPanel(String PanelName) {
        IMyTextPanel Panel = (IMyTextPanel)MyGTS.GetBlockWithName(PanelName);
        if (Panel == null) {
          MyLog("<<Warning>> " + PanelName + " not found");
          WarningFlag = GlobStep + 500;
        } else {
          Panel.WriteText("", false);
        }
      }
      // << ---- O V E R R I D E S ---- >>
      public override cPose GetInputs() {
        cPose Input;
        if (OnOff) {
          if (ReadKeyboard) {
            var KI = GetKeyboardInput();
            if (UseTarget && (KI.Pos.Length() > 0.1 * dt)) {
              UseTarget = false;
              MyLog("'MoveTo' interrupted by keyboard input.");
            }
            Input = KI;
          } else {
            Input = new cPose();
          }
          if (ReadMouse) {
            var MI = GetMouseInput();
            if (UseTarget && (MI.Mat.M11 < 0.999 || MI.Mat.M22 < 0.999 || MI.Mat.M33 < 0.999)) {
              UseTarget = false;
              MyLog("'MoveTo' interrupted by mouse input.");
            }
            Input += GetMouseInput();
          }
          if (ReferenceFrame != null) {
            Input = ApplyReferenceFrame(Input);
          }
          if (UseTarget) {
            Input += MoveToTarget();
          }
          Input += ConstantMovement;
        } else {
          Input = new cPose();
          MyEcho("Controller is Off. You can turn it on by calling \"-OO True\"");
        }
        return Input;  // (dm/dt, drad/dt)

      }  // (m/s, rad/s)
      public override void ExecuteCommand(String Command) {
        //MyLog( Command );
        try {
          String Word;
          var Words = Command.Split(' ');
          double X = 0; double Y = 0; double Z = 0;
          double Yaw = 0; double Pitch = 0; double Roll = 0, HomeSpeed = 1;
          String PanelName;
          string PartName;
          MyLog("'" + Words[0] + "'");
          switch (Words[0]) {
            case "GoHome":
            case "-GH":
            MyLog("GoingHome");
            try {
              HomeSpeed = STD(Words[1]);
            } catch (Exception) { }
            GoHome(HomeSpeed);
            break;
            case "SetHome":
            case "-SH":
            SetHome();
            break;
            case "OnOff":
            case "-OO":
            Word = Words[1];
            OnOff = OnOffToggle(Word, OnOff);
            MyLog("Changing OnOff State to: " + OnOff.ToString());
            break;
            case "ReadKeyboard":
            case "-RK":
            Word = Words[1];
            ReadKeyboard = OnOffToggle(Word, ReadKeyboard);
            break;
            case "ReadMouse":
            case "-RM":
            Word = Words[1];
            ReadMouse = OnOffToggle(Word, ReadMouse);
            break;
            case "PrintPosition":
            case "-PP":
            try {
              PanelName = Reform(Words, 1);
              PrintPosition(PanelName);
            } catch (Exception) {
              PrintPosition();
            }
            break;
            case "ClearPanel":
            case "-CP":
            PanelName = Reform(Words, 1);
            ClearPanel(PanelName);
            break;
            case "Speed":
            case "-S":
            Speed = STD(Words[1]);
            break;
            case "Softness":
            case "-SO":
            Softness = STD(Words[1]);
            break;
            case "Move":
            case "-M":
            UseTarget = false;
            try {
              X = STD(Words[1]);
              Y = STD(Words[2]);
              Z = STD(Words[3]);
              Yaw = STD(Words[4]);
              Pitch = STD(Words[5]);
              Roll = STD(Words[6]);
            } catch (Exception) { }

            Move(X, Y, Z, Yaw, Pitch, Roll);
            break;
            case "MoveTo":
            case "-MT":
            UseTarget = true;
            try {
              X = STD(Words[1]);
              Y = STD(Words[2]);
              Z = STD(Words[3]);
              Yaw = STD(Words[4]);
              Pitch = STD(Words[5]);
              Roll = STD(Words[6]);
            } catch (Exception) { }

            MoveTo(X, Y, Z, Yaw, Pitch, Roll);
            break;
            case "RelMove":
            case "-RelM":
            UseTarget = false;
            try {
              X = STD(Words[1]);
              Y = STD(Words[2]);
              Z = STD(Words[3]);
              Yaw = STD(Words[4]);
              Pitch = STD(Words[5]);
              Roll = STD(Words[6]);
            } catch (Exception) { }
            RelMove(X, Y, Z, Yaw, Pitch, Roll);
            break;
            case "Override":
            case "-O":
            MyLog("Setting Override");
            Word = Words[1];
            PartName = Reform(Words, 2);
            if (HardwareList.ContainsKey(PartName)) {
              HardwareList[PartName].Override = OnOffToggle(Word, HardwareList[PartName].Override);
            } else {
              MyLog("'" + PartName + "' Not found.");
              ErrorFlag = GlobStep + 200;
            }
            break;
            case "SetPartHome":
            case "-SPH":
            Word = Words[1];
            PartName = Reform(Words, 2);
            if (HardwareList.ContainsKey(PartName)) {
              HardwareList[PartName].Home = STD(Words[1]);
              MyLog("Setting " + PartName + "'s Home to: " + HardwareList[PartName].Home);
            } else {
              MyLog("'" + PartName + "' Not found.");
              ErrorFlag = GlobStep + 200;
            }
            break;
          }
        } catch (Exception) {
          MyLog("<<Error>>: Parsing error in '" + Command + "'");
          ErrorFlag = GlobStep + 200;
        }
      }
      // << ---- P R I V A T E   V A R I A B L E S ---- >>
      bool UseTarget;
      cPose target = new cPose();
      uint ContrStep;
      List<IMyTerminalBlock> controllers;
      List<IMyTerminalBlock> Controllers {
        get {
          if (ContrStep < GlobStep - 60) {
            MyGTS.SearchBlocksOfName(ShipControllerKeyword, controllers);
            if (controllers == null) controllers = new List<IMyTerminalBlock>();
            ContrStep = GlobStep;
          }
          return controllers;
        }
      }

      // << ---- P R I V A T E   F U N C T I O N S ---- >>
      cPose GetKeyboardInput() {
        Vector3D IV = new Vector3D();
        // Get the inputs from all desired controllers
        foreach (IMyTerminalBlock Controller in Controllers) {
          var Ctrl = (IMyShipController)Controller;
          if (Ctrl.IsUnderControl) {
            IV.X += -Ctrl.MoveIndicator.Z;
            IV.Y += -Ctrl.MoveIndicator.X;
            IV.Z += Ctrl.MoveIndicator.Y;
          }
        }
        MyDebug("KeyboardInput: " + IV.ToString("0.00"));
        // Normalyse the linear velocity target
        if (IV.Length() > 0.001) IV *= Speed / IV.Length();

        return new cPose(IV * dt);
      }
      cPose GetMouseInput() {
        cPose Input;
        Vector3D IV = new Vector3D();
        // Get the inputs from all desired controllers
        foreach (IMyTerminalBlock Controller in Controllers) {
          var Ctrl = (IMyShipController)Controller;
          if (Ctrl.IsUnderControl) {
            IV.X += Ctrl.RotationIndicator.Y / 40;  // Yaw
            IV.Y += -Ctrl.RotationIndicator.X / 40;  // Pitch
            IV.Z += -Ctrl.RollIndicator * 1;  // Roll
          }
        }
        MyDebug("MouseInput: " + IV.ToString("0.00"));
        // Create the rotation Input
        Input = new cPose(RotZ(Math.Max(-5, Math.Min(5, -IV.X)) * dt * YawSpeed));  // Yaw
        Input = Input + new cPose(RotY(Math.Max(-5, Math.Min(5, -IV.Y)) * dt * PitchSpeed));  // Pitch
        Input = Input + new cPose(RotX(Math.Max(-5, Math.Min(5, -IV.Z)) * dt * RollSpeed));  // Roll
        return Input;
      }

      cPose ApplyReferenceFrame(cPose Input) {
        var Targete = ReferenceFrame.Pose + Input - ReferenceFrame.Pose;
        var IMat = MatrixD.Invert(ReferenceFrame.Pose.Mat);
        var dX = Input.Mat.M41;
        var dY = Input.Mat.M42;
        var dZ = Input.Mat.M43;
        Targete.Pos = new Vector3D(
          (dX * IMat.M11) + (dY * IMat.M12) + (dZ * IMat.M13),
          (dX * IMat.M21) + (dY * IMat.M22) + (dZ * IMat.M23),
          (dX * IMat.M31) + (dY * IMat.M32) + (dZ * IMat.M33));
        return Targete;
      }
      cPose MoveToTarget() {
        var Diff = Target - Arm.Pose;
        Diff *= dt * 0.5;
        Diff.Pos = (Target.Pos - Arm.Pose.Pos) * dt;
        var Dist = Diff.Pos.Length();
        if (Dist > 0.05) {
          Diff.Pos = Diff.Pos * Speed / Math.Max(1, Dist * 2) * 2 * dt;
          return Diff;
        } else {
          Diff.Pos = new Vector3D();
          return Diff;
        }
      }
    }
  }
}