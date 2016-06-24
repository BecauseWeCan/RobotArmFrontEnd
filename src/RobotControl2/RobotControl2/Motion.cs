using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using SupportLib;

namespace SynqNetBot
{
    public partial class Motion : Form
    {
        public Robot.HomeState home_state = Robot.HomeState.Unhomed;
        public Robot.AxisHomeState[] home_axis_state = new Robot.AxisHomeState[Robot.ROBOT_AXES];
        public int homing_axis = 0;
        public Controller controller;
        public XBoxController xbox_controller;
        private bool status_enabled;
        private bool jogging_enabled;
        private bool homing_enabled;
        private bool shutdown;
        private ShutdownState shutdown_state = ShutdownState.StartShutdown;
        private string ConfigPath;
        private Config config;

        private const double TIMER_UPDATE = 0.1; // seconds
        public int robot = 0;
        private Axis[] Axis = new Axis[Robot.ROBOT_AXES];

        public const double TWOPI = 2.0 * Math.PI;
        public const double PIOVERTWO = Math.PI / 2.0;
        public const double RADPERDEG = Math.PI / 180.0;
        public const double DEGPERRAD = 180.0 / Math.PI;
        public const double PI = Math.PI;
        public const double PI2 = PI / 2;


        private double L1;
        private double L2;
        private double L3;
        private double L4;
        private double L5;
        private double L6;
        private double L5A;
        private double AT5;
        private double min_uw;
        private double max_uw;
        private MotionConfig motion_config;

        private static double[] comm_offset = new double[16]
        {
	        -157.5,
	        -135,
	        180,
	        157.5,
	        -90,
	        -112.5,
	        -67.5,
	        -45,
	        90,
	        67.5,
	        112.5,
	        135,
	        22.5,
	        45,
	        0,
	        -22.5,
        };

        private enum ShutdownState
        {
            Idle,
            StartShutdown,
            StopMotion,
            EnableBrake,
            DisableDrive,
            Done,
        }

        private enum MotionConfig
        {
            Unconfigured,
            SingleAxis,
            MultiAxis,
        }
        public Motion(int RobotNumber)
        {
            InitializeComponent();
            status_enabled = false;
            jogging_enabled = false;
            homing_enabled = false;
            shutdown = false;
            robot = RobotNumber;
            groupBox1.Text = "Robot " + robot.ToString("0");
            motion_config = MotionConfig.Unconfigured;
            controller = new Controller();
            xbox_controller = new XBoxController(RobotNumber);
            uint error_code = controller.Initialize(robot, false);
            Debug.Print("Initialize returns " + error_code);
            if(error_code == 0)
            {
                timer1.Enabled = true;
                timer1.Interval = (int)(1000 * TIMER_UPDATE); // Milliseconds 
            }
        }

        // Config should be called every time a robot is created.
        // Init should only be called after a controller reset.

        public void Config(String Path)
        {
            // Robot Geometry Parameters
            ConfigPath = Path;
            String RobotConfigFile = Path + "Robot.cfg";
            Debug.Print("Robot " + robot + " Configuration: (from file " + RobotConfigFile + ")");

            config = new Config();
            config.GetConfig(RobotConfigFile, "Link 1", out L1);
            config.GetConfig(RobotConfigFile, "Link 2", out L2);
            config.GetConfig(RobotConfigFile, "Link 3", out L3);
            config.GetConfig(RobotConfigFile, "Link 4", out L4);
            config.GetConfig(RobotConfigFile, "Link 5", out L5);
            config.GetConfig(RobotConfigFile, "Link 6", out L6);
            config.GetConfig(RobotConfigFile, "Min UW Sum", out min_uw);
            config.GetConfig(RobotConfigFile, "Max UW Sum", out max_uw);
            L5A = Math.Sqrt(L5 * L5 + L4 * L4);
            AT5 = Math.Atan2(L4, L5);
            Debug.Print("\n");
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                Debug.Print("Axis Number " + axis + " Configuration:");
                String AxisConfigFile = Path + "Axis" + axis + ".cfg";
                Axis[axis] = new Axis(AxisConfigFile);
                Debug.Print("\n");
            }

            uint error_code = controller.ConfigSingleAxis();
            Debug.Print("ConfigSingleAxis returns " + error_code);

            motion_config = MotionConfig.SingleAxis;
        }
        public void UseCurrentOrigins()
        {
            String OriginFile = ConfigPath + "Origins.cfg";
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                double origin;
                config.GetConfig(OriginFile, "Origin " + axis, out origin);
                Axis[axis].Origin = origin;
            }
            home_state = Robot.HomeState.Homed;
        }

        public void Initialize(String Path)
        {
            uint error_code = controller.SetCommutationOffsets();
            Debug.Print("SetCommutationOffsets returns " + error_code);
            error_code = controller.IntializeDefaults();
            Debug.Print("IntializeDefaults returns " + error_code);
            // Motion and Axes
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                error_code = controller.InitializeAxis(axis, 
                    Math.Abs(Axis[axis].error_limit * Axis[axis].COUNTS_PER_DEGREE),
                    Math.Abs(Axis[axis].position_tolerance * Axis[axis].COUNTS_PER_DEGREE),
                    Axis[axis].stop_time,
                    Axis[axis].estop_time);

                Debug.Print("IntializeAxis(" + axis + ") returns " + error_code);

                error_code = controller.SetGain(axis, Gain.Kp, Axis[axis].Kp);
                Debug.Print("SetGain(" + axis + ") for Kp returns " + error_code);
                error_code = controller.SetGain(axis, Gain.Ki, Axis[axis].Ki);
                Debug.Print("SetGain(" + axis + ") for Ki returns " + error_code);
                error_code = controller.SetGain(axis, Gain.Kd, Axis[axis].Kd);
                Debug.Print("SetGain(" + axis + ") for Kd returns " + error_code);
                error_code = controller.SetGain(axis, Gain.Kvff, Axis[axis].Kvff);
                Debug.Print("SetGain(" + axis + ") for Kvff returns " + error_code);
                error_code = controller.SetGain(axis, Gain.Kaff, Axis[axis].Kaff);
                Debug.Print("SetGain(" + axis + ") for Kaff returns " + error_code);
            }
            error_code = controller.ConfigSingleAxis();
            Debug.Print("ConfigSingleAxis returns " + error_code);

            motion_config = MotionConfig.SingleAxis;
        }

        public Robot.MotionState MotionState(int axis)
        {
            SupportLib.MotionState motion_state = controller.GetMotionState(axis);
            Robot.MotionState state = Robot.MotionState.Moving;
            if (motion_state == SupportLib.MotionState.Done) state = Robot.MotionState.Done;
            if (motion_state == SupportLib.MotionState.Error) state = Robot.MotionState.Error;
            return state;
        }
        public void MoveAll(double[] x, double speed, double accel)
        {
            if (home_state != Robot.HomeState.Homed)
            {
                Debug.Print("Cannot move until robot is homed");
            }
            else
            {
                int axis;
                double[] tg = new double[Robot.ROBOT_AXES];
                double sp = speed * Axis[0].COUNTS_PER_DEGREE;
                if (sp < 0.0) sp = -sp;
                double ac = accel * Axis[0].COUNTS_PER_DEGREE;
                if (ac < 0.0) ac = -ac;

                jogging_enabled = false;

                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                {
                    if (x[axis] < Axis[axis].local_min_position)
                    {
                        Debug.Print("Position (" + x[axis] + ") is less than minimium position for axis " + axis);
                        return;
                    }
                    if (x[axis] > Axis[axis].local_max_position)
                    {
                        Debug.Print("Position (" + x[axis] + ") is greater than maximium position for axis " + axis);
                        return;
                    }
                    Axis[axis].LocalToRaw(x[axis], out tg[axis]);
                }
                Robot.MotionState m = MotionState(0);
                switch (m)
                {
                    case Robot.MotionState.Done:
                        uint error_code = controller.ConfigMultiAxis();
                        Debug.Print("ConfigMultiAxis returns " + error_code);

                        motion_config = MotionConfig.MultiAxis;
                        error_code = controller.SCurve(axis, tg, sp, ac, ac);
                Debug.Print("SCurve returns " + error_code);
                        break;
                    case Robot.MotionState.Error:
                        break;
                    case Robot.MotionState.Moving:
                        error_code = controller.SCurve(axis, tg, sp, ac, ac);
                Debug.Print("SCurve returns " + error_code);
                        break;
                }
            }
        }
        public void MoveXYZ(Robot.WorldCoords world, double speed, double accel)
        {
            double[] x = new double[Robot.ROBOT_AXES];
            WorldToLocal(world, x);
            MoveAll(x, speed, accel);
        }

        public void MovePVT(int axis, double[] position, double[] velocity, double[] time)
        {
            if (MotionState(axis) == Robot.MotionState.Done)
            {
                uint error_code = controller.ConfigSingleAxis();
                Debug.Print("ConfigSingleAxis returns " + error_code);

                int i;
                double[] x = new double[position.Length];
                double[] v = new double[position.Length];
                for (i = 0; i < position.Length; i++)
                {
                    Axis[axis].LocalToRaw(position[i], out x[i]);
                    v[i] = velocity[i] * Axis[axis].COUNTS_PER_DEGREE;
                }
                error_code = controller.PVT(axis, position.Length, x, v, time);
                Debug.Print("BSpline returns " + error_code);
            }
        }

        public void MoveBSpline(int axis, double[] position, double[] time)
        {
            if (MotionState(axis) == Robot.MotionState.Done)
            {
                uint error_code = controller.ConfigSingleAxis();
                Debug.Print("ConfigSingleAxis returns " + error_code);

                int i;
                double[] x = new double[position.Length];
                for (i = 0; i < position.Length; i++)
                {
                    Axis[axis].LocalToRaw(position[i], out x[i]);
                }
                error_code = controller.BSpline(axis, position.Length, x, time);
                Debug.Print("BSpline returns " + error_code);
            }
        }

        public void MoveAxis(int axis, double target, double speed, double accel)
        {
            if (MotionState(axis) == Robot.MotionState.Done)
            {
                if (target < Axis[axis].local_min_position)
                {
                    Debug.Print("Position (" + target + ") is less than minimium position for axis " + axis);
                    return;
                }
                if (target > Axis[axis].local_max_position)
                {
                    Debug.Print("Position (" + target + ") is greater than maximium position for axis " + axis);
                    return;
                }

                uint error_code = controller.ConfigSingleAxis();
                Debug.Print("ConfigSingleAxis returns " + error_code);

                double sp = speed * Axis[axis].COUNTS_PER_DEGREE;
                if (sp < 0.0) sp = -sp;
                double ac = accel * Axis[axis].COUNTS_PER_DEGREE;
                if (ac < 0.0) ac = -ac;
                double tg;
                Axis[axis].LocalToRaw(target, out tg);
                error_code = controller.SCurve(axis, tg, sp, ac, ac);
                Debug.Print("SCurve returns " + error_code);
            }
        }
        private void MoveAxis(int axis, double target)
        {
            double speed = Axis[axis].jog_speed;
            double accel = Axis[axis].jog_accel;
            MoveAxis(axis, target, speed, accel);
        }
        public void SetFeedRate(int axis, double FeedRate)
        {
            controller.SetFeedrate(axis, FeedRate);
        }
        public double GetFeedRate(int axis)
        {
            return controller.GetFeedrate(axis);
        }
        public void ClearFault()
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                ClearFault(axis);
            }
        }
        public double MoveTime(int axis)
        {
            double et = controller.GetMoveTime(axis);
            return et / 2000.0;
        }
        public void ClearFault(int axis)
        {
            controller.FaultsClear(axis);
        }
        public void Stop()
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                Stop(axis);
            }
        }
        public void Stop(int axis)
        {
            SupportLib.MotionState motion_state = controller.GetMotionState(axis);

            if (motion_state == SupportLib.MotionState.Moving)
            {
                controller.Stop(axis);
            }
        }
        public void EStop()
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                EStop(axis);
            }
        }
        public void EStop(int axis)
        {
            SupportLib.MotionState motion_state = controller.GetMotionState(axis);

            if (motion_state == SupportLib.MotionState.Moving)
            {
                controller.EStop(axis);
            }
        }
        public void Enable(int axis)
        {
            controller.FaultsClear(axis);
            controller.Enable(axis, true);
        }
        public void Enable()
        {
            controller.MotorOutSet(0, 2, false);
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                Enable(axis);
            }
        }
        public void Shutdown()
        {
            shutdown_state = ShutdownState.StartShutdown;
            shutdown = true;
        }
        private void ShutdownExecute()
        {
            int axis;
            switch (shutdown_state)
            {
                case ShutdownState.Idle:
                    break;
                case ShutdownState.StartShutdown:
                    jogging_enabled = false;
                    status_enabled = false;
                    homing_enabled = false;
                    shutdown_state = ShutdownState.StopMotion;
                    break;
                case ShutdownState.StopMotion:
                    EStop();
                    shutdown_state = ShutdownState.EnableBrake;
                    break;
                case ShutdownState.EnableBrake:
                    controller.MotorOutSet(0, 2, true);
                    shutdown_state = ShutdownState.DisableDrive;
                    break;
                case ShutdownState.DisableDrive:
                    for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                    {
                        controller.Enable(axis, false);
                    }
                    shutdown_state = ShutdownState.Done;
                    break;
                case ShutdownState.Done:
                    shutdown = false;
                    break;
                default:
                    break;
            }
        }

        private void HomeAxis(int axis)
        {
            uint error_code = 0;
            switch (Axis[axis].home_type)
            {
                case 0:
                    // home is here
                    Axis[axis].Origin = controller.ActualPosition(axis) -
                        Axis[axis].home_offset * Axis[axis].COUNTS_PER_DEGREE;
                    home_axis_state[axis] = Robot.AxisHomeState.Homed;
                    break;
                case 1:
                    {
                        double hs = Axis[axis].home_speed * Axis[axis].COUNTS_PER_DEGREE;
                        double ha = Axis[axis].home_accel * Axis[axis].COUNTS_PER_DEGREE;
                        if (ha < 0) ha = -ha;
                        double hl = Axis[axis].home_error_limit * Axis[axis].COUNTS_PER_DEGREE;
                        if (hl < 0) hl = -hl;
                        Robot.AxisHomeState state = home_axis_state[axis];
                        Robot.MotionState axis_state;

                        switch (state)
                        {
                            case Robot.AxisHomeState.Idle:
                            case Robot.AxisHomeState.Error:
                            case Robot.AxisHomeState.Homed:
                                break;
                            case Robot.AxisHomeState.Start: //Wait for any moves in progress to complete
                                axis_state = MotionState(axis);
                                if (axis_state == Robot.MotionState.Done)
                                {
                                    state = Robot.AxisHomeState.MoveToStop;
                                }
                                if (axis_state == Robot.MotionState.Error)
                                {
                                    state = Robot.AxisHomeState.Error;
                                }
                                break;
                            case Robot.AxisHomeState.MoveToStop:
                                error_code = controller.ConfigureUserLimit(axis, hs, hl);
                                Debug.Print("ConfigureUserLimit returns " + error_code);
                                controller.Velocity(axis, hs, ha);                           
                                state = Robot.AxisHomeState.WaitForStop;
                                break;
                            case Robot.AxisHomeState.WaitForStop:
                                axis_state = MotionState(axis);
                                if (axis_state == Robot.MotionState.Done)
                                {
                                    state = Robot.AxisHomeState.MoveToIndex;
                                    error_code = controller.EnableUserLimit(axis, false);
                                    Debug.Print("EnableUserLimit returns " + error_code);
                                }
                                if (axis_state == Robot.MotionState.Error)
                                {
                                    state = Robot.AxisHomeState.Error;
                                }
                                break;
                            case Robot.AxisHomeState.MoveToIndex:
                                controller.CaptureArm(axis, true);
                                controller.Velocity(axis, -hs, ha);                           
                                state = Robot.AxisHomeState.WaitForIndex;
                                break;
                            case Robot.AxisHomeState.WaitForIndex:
                                axis_state = MotionState(axis);
                                CaptureState capture_state = controller.CaptureState(axis);
                                if (capture_state == CaptureState.Captured)
                                {
                                    Stop(axis);
                                    double capturePosition = controller.CapturePosition(axis);
//                                    capture.ConfigurationReset();
                                    Debug.Print("Captured Position for axis " + axis + " = " + capturePosition);
                                    Axis[axis].Origin = capturePosition - Axis[axis].home_offset * Axis[axis].COUNTS_PER_DEGREE;
                                    state = Robot.AxisHomeState.StopMotion;
                                }
                                if (axis_state == Robot.MotionState.Done)
                                {
                                    if (capture_state != CaptureState.Captured)
                                    {
                                        state = Robot.AxisHomeState.Error;
                                    }
                                }
                                if (axis_state == Robot.MotionState.Error)
                                {
                                    state = Robot.AxisHomeState.Error;
                                }
                                break;
                            case Robot.AxisHomeState.StopMotion:
                                axis_state = MotionState(axis);
                                if (axis_state == Robot.MotionState.Done)
                                {
                                    state = Robot.AxisHomeState.MoveHome;
                                }
                                if (axis_state == Robot.MotionState.Error)
                                {
                                    state = Robot.AxisHomeState.Error;
                                }
                                break;
                            case Robot.AxisHomeState.MoveHome:
                                MoveAxis(axis, Axis[axis].home_position);
                                state = Robot.AxisHomeState.WaitForHome;
                                break;
                            case Robot.AxisHomeState.WaitForHome:
                                axis_state = MotionState(axis);
                                if (axis_state == Robot.MotionState.Done)
                                {
                                    state = Robot.AxisHomeState.Homed;
                                }
                                if (axis_state == Robot.MotionState.Error)
                                {
                                    state = Robot.AxisHomeState.Error;
                                }
                                break;
                            default:
                                break;
                        }
                        home_axis_state[axis] = state;
                    }
                    break;
                default:
                    break;
            }
        }
        private void Home()
        {
            int axis;
            switch (home_state)
            {
                case Robot.HomeState.Begin:
                    for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                    {
                        home_axis_state[axis] = Robot.AxisHomeState.Idle;
                    }
                    jogging_enabled = false;
                    homing_axis = 0;
                    home_axis_state[homing_axis] = Robot.AxisHomeState.Start;
                    home_state = Robot.HomeState.Homing;
                    break;
                case Robot.HomeState.Homing:
                    HomeAxis(homing_axis);
                    switch (home_axis_state[homing_axis])
                    {
                        case Robot.AxisHomeState.Error:
                            home_state = Robot.HomeState.Error;
                            homing_enabled = false;
                            break;
                        case Robot.AxisHomeState.Homed:
                            homing_axis++;
                            if (homing_axis >= Robot.ROBOT_AXES)
                            {
                                homing_enabled = false;
                                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                {
                                    Axis[axis].LocalToRaw(Axis[axis].local_min_position, out Axis[axis].raw_min_position);
                                    Axis[axis].LocalToRaw(Axis[axis].local_max_position, out Axis[axis].raw_max_position);
                                }
                                String OriginFile = ConfigPath + "Origins.cfg";
                                StreamWriter sw = new StreamWriter(OriginFile);
                                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                {
                                    sw.WriteLine("Origin " + axis + " = " + Axis[axis].Origin);
                                }
                                sw.Close();
                                home_state = Robot.HomeState.Homed;
                            }
                            else
                            {
                                home_axis_state[homing_axis] = Robot.AxisHomeState.Start;
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case Robot.HomeState.Homed:
                    break;
                default:
                    break;
            }
        }
        public void EnableJogging(bool enable)
        {
            if (enable)
            {
                if (motion_config != MotionConfig.SingleAxis)
                {
                    controller.ConfigSingleAxis();
                    motion_config = MotionConfig.SingleAxis;
                }
            }
            jogging_enabled = enable;
        }
        public void EnableStatus(bool enable)
        {
            status_enabled = enable;
        }
        public void StartHoming()
        {
            if (motion_config != MotionConfig.SingleAxis)
            {
                controller.ConfigSingleAxis();
                motion_config = MotionConfig.SingleAxis;
            }
            home_state = Robot.HomeState.Begin;
            homing_enabled = true;
        }
        private void Jog(int axis, double jog_v)
        {
            double js = jog_v * Axis[axis].COUNTS_PER_DEGREE;
            double ja = Axis[axis].jog_accel * Axis[axis].COUNTS_PER_DEGREE;
            if (ja < 0.0) ja = -ja;

            if ((home_state == Robot.HomeState.Homed) && (js != 0.0))
            {
                double end_distance;
                double current_position = controller.ActualPosition(axis);
                if (jog_v > 0)
                {
                    end_distance = Axis[axis].raw_max_position - current_position;
                    if (js < 0)
                    {
                        end_distance = -end_distance;
                    }
                    if (end_distance <= 0)
                    {
                        js = 0;
                    }
                    else
                    {
                        double vmax = Math.Sqrt(end_distance * ja);
                        if (js < 0)
                        {
                            if (js < -vmax) js = -vmax;
                        }
                        else
                        {
                            if (js > vmax) js = vmax;
                        }
                    }
                }
                else
                {
                    end_distance = current_position - Axis[axis].raw_min_position;
                    if (js > 0)
                    {
                        end_distance = -end_distance;
                    }
                    if (end_distance <= 0)
                    {
                        js = 0;
                    }
                    else
                    {
                        double vmax = Math.Sqrt(end_distance * ja);
                        if (js < 0)
                        {
                            if (js < -vmax) js = -vmax;
                        }
                        else
                        {
                            if (js > vmax) js = vmax;
                        }
                    }
                }
            }
            
            if (controller.GetMotionState(axis) != SupportLib.MotionState.Error)
            {
                controller.Velocity(axis, js, ja);
            }
        }
        private void JogRobot()
        {
            if(xbox_controller.IsConnected())
            {
                double jv = xbox_controller.Thumbstick(Thumbstick.RIGHT_X) * Axis[0].jog_speed;
                Jog(0, jv);
                jv = xbox_controller.Thumbstick(Thumbstick.RIGHT_Y) * Axis[1].jog_speed;
                Jog(1, jv);
                jv = xbox_controller.Thumbstick(Thumbstick.LEFT_Y) * Axis[2].jog_speed;
                Jog(2, jv);
                jv = xbox_controller.Thumbstick(Thumbstick.LEFT_X) * Axis[3].jog_speed;
                Jog(3, jv);

                jv = 0;
                if (xbox_controller.Button(SupportLib.Button.DPAD_UP))
                {
                    jv = Axis[4].jog_speed;
                }
                if (xbox_controller.Button(SupportLib.Button.DPAD_DOWN))
                {
                    jv = -Axis[4].jog_speed;
                }
                Jog(4, jv);

                jv = 0;
                if (xbox_controller.Button(SupportLib.Button.DPAD_LEFT))
                {
                    jv = Axis[5].jog_speed;
                }
                if (xbox_controller.Button(SupportLib.Button.DPAD_RIGHT))
                {
                    jv = -Axis[5].jog_speed;
                }
                Jog(5, jv);
            }
        }

        private void UpdateForm()
        {
            Robot.WorldCoords w = new Robot.WorldCoords();
            double[] r = new double[Robot.ROBOT_AXES];
            double[] l = new double[Robot.ROBOT_AXES];

            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                r[axis] = controller.ActualPosition(axis);
                Axis[axis].RawToLocal(r[axis], out l[axis]);
            }

            LocalToWorld(l, out w);

            //// display U, W limits for debug

            //Axis.RawToLocal(1, raw_min_position[1], out w.x);
            //Axis.RawToLocal(1, raw_max_position[1], out w.y);
            //Axis.RawToLocal(2, raw_min_position[2], out w.z);
            //Axis.RawToLocal(2, raw_max_position[2], out w.u);

            // raw
            textBox1.Text = r[0].ToString("0");
            textBox2.Text = r[1].ToString("0");
            textBox3.Text = r[2].ToString("0");
            textBox4.Text = r[3].ToString("0");
            textBox5.Text = r[4].ToString("0");
            textBox6.Text = r[5].ToString("0");

            // local
            textBox12.Text = l[0].ToString("0.0");
            textBox11.Text = l[1].ToString("0.0");
            textBox10.Text = l[2].ToString("0.0");
            textBox9.Text = l[3].ToString("0.0");
            textBox8.Text = l[4].ToString("0.0");
            textBox7.Text = l[5].ToString("0.0");

            // world
            textBox18.Text = w.x.ToString("0.0");
            textBox17.Text = w.y.ToString("0.0");
            textBox16.Text = w.z.ToString("0.0");
            textBox15.Text = w.u.ToString("0.0");
            textBox14.Text = w.v.ToString("0.0");
            textBox13.Text = w.w.ToString("0.0");
        }
        void UpdateUWLimits()
        {
            double current_w = controller.ActualPosition(1);
            double current_u = controller.ActualPosition(2);
            double u, w;
            Axis[1].RawToLocal(current_w, out w);
            Axis[2].RawToLocal(current_u, out u);
            if ((u + w) < min_uw)
            {

                double w_min = min_uw - u;
                double u_min = min_uw - w;
                Axis[1].LocalToRaw(w_min, out Axis[1].raw_min_position);
                Axis[2].LocalToRaw(u_min, out Axis[2].raw_min_position);
            }
            else if ((u + w) > max_uw)
            {
                double w_max = max_uw - u;
                double u_max = max_uw - w;
                Axis[1].LocalToRaw(w_max, out Axis[1].raw_max_position);
                Axis[2].LocalToRaw(u_max, out Axis[2].raw_max_position);
            }
            else
            {
                Axis[1].LocalToRaw(Axis[1].local_max_position, out Axis[1].raw_max_position);
                Axis[1].LocalToRaw(Axis[1].local_min_position, out Axis[1].raw_min_position);
                Axis[2].LocalToRaw(Axis[2].local_max_position, out Axis[2].raw_max_position);
                Axis[2].LocalToRaw(Axis[2].local_min_position, out Axis[2].raw_min_position);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (shutdown)
            {
                ShutdownExecute();
            }
            if (jogging_enabled)
            {
                JogRobot();
            }
            if (status_enabled)
            {
                UpdateForm();
            }
            if (homing_enabled)
            {
                Home();
            }
            if (home_state == Robot.HomeState.Homed)
            {
                UpdateUWLimits();
            }
        }
        public void LocalToWorld(double[] local, out Robot.WorldCoords world)
        {
            if (home_state == Robot.HomeState.Homed)
            {
                double ta = local[0] * RADPERDEG;
                double wa = local[1] * RADPERDEG;
                double ua = local[2] * RADPERDEG;
                double r = L3 * Math.Sin(wa) + L5 * Math.Cos(ua) - L4 * Math.Sin(ua) + L2;
                world.x = r * Math.Cos(ta);
                world.y = r * Math.Sin(ta);
                world.z = L3 * Math.Cos(wa) + L5 * Math.Sin(ua) + L4 * Math.Cos(ua) + L1;
            }
            else
            {
                world.x = 0.0;
                world.y = 0.0;
                world.z = 0.0;
            }
            world.u = local[3];
            world.v = local[4] + local[2];
            world.w = local[5];
        }

        public void WorldToLocal(Robot.WorldCoords world, double[] local)
        {
            //int get_theta_uw(double x, double y, double z, double *theta, double *u, double *w)
            //{
            //    double ra, za, suw, uw, r, r2, B, bs, ta, ua, wa;

            //    if((z < LIMIT_NEG_Z) || (z > LIMIT_POS_Z)) return 1;
            //    ra =  sqrt(x * x + y * y);
            //    if((ra < LIMIT_NEG_R) || (ra > LIMIT_POS_R)) return 2; 
            //    ta = atan2(y,x);
            //    if((ta < LIMIT_NEG_THETA) || (ta > LIMIT_POS_THETA)) return 3; 
            //    za =  z - L1;
            //    ra -= L2;

            //    r2 = ra * ra + za * za;
            //    r = sqrt(r2);
            //    suw = (r2 - (L3 * L3 + L5A * L5A)) / (2 * L3 * L5A);
            //    uw = asin(suw);
            //    bs = L3 * cos(uw) / r;
            //    B = asin(bs);
            //    ua = atan2(za,ra) - B;
            //    wa = uw - ua;
            //    ua -= AT5;
            //    if((ua < LIMIT_NEG_U) || (ua > LIMIT_POS_U)) return 4; 
            //    if((wa < LIMIT_NEG_W) || (wa > LIMIT_POS_W)) return 5; 
            //    uw = ua + wa;
            //    if((uw < LIMIT_NEG_UW) || (uw > LIMIT_POS_UW)) return 6; 

            //    *theta = ta;
            //    *u = ua;
            //    *w = wa;
            //    return 0;
            //}
            // for now
            //    double za, suw, uw, r, r2, B, bs, ta, ua, wa;
            double ra = Math.Sqrt(world.x * world.x + world.y * world.y);
            double ta = Math.Atan2(world.y, world.x);
            double za = world.z - L1;
            ra -= L2;
            double r2 = ra * ra + za * za;
            double r = Math.Sqrt(r2);
            double suw = (r2 - (L3 * L3 + L5A * L5A)) / (2 * L3 * L5A);
            double uw = Math.Asin(suw);
            double bs = L3 * Math.Cos(uw) / r;
            double B = Math.Asin(bs);
            double ua = Math.Atan2(za, ra) - B;
            double wa = uw - ua;
            ua -= AT5;

            local[0] = ta * DEGPERRAD;
            local[1] = wa * DEGPERRAD;
            local[2] = ua * DEGPERRAD;
            //double su = Math.Sin(world.u * RADPERDEG);
            //double cu = Math.Cos(world.u * RADPERDEG);
            //double sv = Math.Sin(world.v * RADPERDEG);
            //double cv = Math.Cos(world.v * RADPERDEG);
            //double tx = su * cv;
            //double ty = su * sv;
            //double tz = cu;

            //double st = Math.Sin(PI2 - ua);
            //double ct = Math.Cos(PI2 - ua);
            //double sp = Math.Sin(ta);
            //double cp = Math.Cos(ta);

            //double rt = tx * cp * ct + ty * sp * ct - tz * st;
            //double rp = -tx * sp + ty * cp;
            //double rr = tx * cp * st + ty * sp * st + tz * ct;

            //double a = world.w;
            //double b = -Math.Acos(rr);
            //double c = Math.Atan2(rt, rp) - PI2;

            //local[3] = c * DEGPERRAD;
            //local[4] = b * DEGPERRAD;
            // temporary algorithm
            local[3] = world.u;
            local[4] = world.v - local[2];
            local[5] = world.w;
        }
        public void LocalPosition(double[] local)
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                double raw = controller.ActualPosition(axis);
                Axis[axis].RawToLocal(raw, out local[axis]);
            }
        }
        public void LocalCommandPosition(double[] local)
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                double raw = controller.CommandPosition(axis);
                Axis[axis].RawToLocal(raw, out local[axis]);
            }
        }
        public void WorldPosition(out Robot.WorldCoords world)
        {
            double[] local = new double[Robot.ROBOT_AXES];
            LocalPosition(local);
            LocalToWorld(local, out world);
        }
        public void StartPath(double x, double y, double velocity, double acceleration)
        {
            uint error_code = controller.StartPath(x, y, velocity, acceleration);
            Debug.Print("StartPath returns " + error_code);
        }
        public void StartPath(double x, double y, double z, double velocity, double acceleration)
        {
            uint error_code = controller.StartPath(x, y, z, velocity, acceleration);
            Debug.Print("StartPath returns " + error_code);
        }
        public void FinishPath(out double[] x, out double[] y, out uint count)
        {
            count = controller.FinishPath();
            Debug.Print("FinishPath returns " + count);
            if (count > 0)
            {
                int i;
                x = new double[count];
                y = new double[count];
                for (i = 0; i < count; i++)
                {
                    x[i] = controller.PathX(i);
                    y[i] = controller.PathY(i);
                }
            }
            else
            {
                x = null;
                y = null;
            }
        }
        public void FinishPath(out double[] x, out double[] y, out double[] z, out uint count)
        {
            count = controller.FinishPath();
            Debug.Print("FinishPath returns " + count);
            if (count > 0)
            {
                int i;
                x = new double[count];
                y = new double[count];
                z = new double[count];
                for (i = 0; i < count; i++)
                {
                    x[i] = controller.PathX(i);
                    y[i] = controller.PathY(i);
                    z[i] = controller.PathZ(i);
                }
            }
            else
            {
                x = null;
                y = null;
                z = null;
            }
        }
        public void AddPoint(double x, double y)
        {
            int error_code = controller.AddPoint(x, y);
            Debug.Print("AddPoint returns " + error_code);
        }
        public void AddPoint(double x, double y, double z)
        {
            int error_code = controller.AddPoint(x, y, z);
            Debug.Print("AddPoint returns " + error_code);
        }

        public void AddArc(double x, double y, double angle)
        {
            int error_code = controller.AddArc(x, y, angle);
            Debug.Print("AddArc returns " + error_code);
        }

        public void CutterOn(bool enable)
        {
            controller.DigitalOutSet(0, enable);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}