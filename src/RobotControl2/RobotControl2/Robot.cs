using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
namespace SynqNetBot
{
    // Robot Driver for Dual Fanuc S-500 arms. Written by Bob Steele (started) 1/7/2009.
    // Robot Driver for Dual Fanuc S-500 arms. Edited by Be Han (started) 7/7/2016.

    public class Robot : IRobot
    {
        public const int ROBOT_AXES = 6;

        private Motion motion;
        
        public SupportLib.Controller controller;
        private int robot = 0;
        private bool jog_enable = false;
        private bool status_enable = false;
        private String path;

        public struct WorldCoords
        {
            public double x;    //mm
            public double y;    //mm
            public double z;    //mm
            public double u;    //deg
            public double v;    //deg
            public double w;    //deg
        };

        public enum MotionState
        {
            Done,
            Moving,
            Error,
        }

        public enum HomeState
        {
            Unhomed,
            Begin,
            Homing,
            Homed,
            Error,
        }
        public enum AxisHomeState
        {
            Idle,
            Start,
            MoveToStop,
            WaitForStop,
            MoveToIndex,
            WaitForIndex,
            StopMotion,
            MoveHome,
            WaitForHome,
            Homed,
            Error,
        }

        #region Methods ------------------------------------------------------------------------------
        #region Constructor ---------------------------------------------------
        public Robot(int RobotNumber)
        {
            robot = RobotNumber;
            motion = new Motion(robot);
        }
        #endregion Constructor
        public void Config(String ConfigPath)
        {
            // General Robot Parameters
            path = ConfigPath;
            motion.Config(path);
        }
        public void UseCurrentOrigins()
        {
            motion.UseCurrentOrigins();
        }
        public void Initialize()
        {
            motion.Initialize(path);
        }
        public void Home()
        {
            motion.StartHoming();
        }
        public void Home35()
        {
            motion.StartHoming35();
        }
        public void Move(Robot.WorldCoords world, double speed, double accel)
        {
            motion.MoveXYZ(world, speed, accel);
        }
        public void Move(double[] x, double speed, double accel)
        {
            motion.MoveAll(x, speed, accel);
        }

        public void MoveAxis(int axis, double x, double speed, double accel)
        {
            motion.MoveAxis(axis, x, speed, accel);
        }

        public void MovePVT(int axis, double[] position, double[] velocity, double[] time)
        {
            motion.MovePVT(axis, position, velocity, time);
        }
        public void MoveBSpline(int axis, double[] position, double[] time)
        {
            motion.MoveBSpline(axis, position, time);
        }
        public void SetFeedRate(int axis, double FeedRate)
        {
            motion.SetFeedRate(axis, FeedRate);
        }
        public double GetFeedRate(int axis)
        {
            return motion.GetFeedRate(axis);
        }
        public void ClearFault()
        {
            motion.ClearFault();
        }
        public double MoveTime(int axis)
        {
            return motion.MoveTime(axis);
        }
        public void ClearFault(int axis)
        {
            motion.ClearFault(axis);
        }
        public void Stop()
        {
            motion.Stop();
        }
        public void Stop(int axis)
        {
            motion.Stop(axis);
        }
        public void EStop()
        {
            motion.EStop();
        }
        public void EStop(int axis)
        {
            motion.EStop(axis);
        }
        public void Enable()
        {
            motion.Enable();
        }
        public void Enable(int axis)
        {
            motion.Enable(axis);
        }
        public void Shutdown()
        {
            motion.Shutdown();
        }
        //public void ControllerEvent(Mpx.EventType type, int number, int[] int32Info, long[] int64Info)
        //{
        //    motion.ControllerEvent(type, number, int32Info, int64Info);
        //}
        #endregion Methods
        #region Properties----------------------------------------------------------------------------
        public bool JoggingEnabled
        {
            get
            {
                Debug.Write("JoggingEnabled get(" + jog_enable + ")\n");
                return jog_enable;
            }
            set
            {
                Debug.Write("JoggingEnabled set(" + value + ")\n");
                jog_enable = value;
                if (jog_enable)
                {
                    motion.EnableJogging(true);
                }
                else
                {
                    motion.EnableJogging(false);
                }
            }
        }

        public bool StatusEnabled
        {
            get
            {
                Debug.Write("StatusEnabled get(" + status_enable + ")\n");
                return status_enable;
            }
            set
            {
                Debug.Write("StatusEnabled set(" + value + ")\n");
                status_enable = value;
                if (status_enable)
                {
                    motion.Show();
                    motion.EnableStatus(true);
                }
                else
                {
                    motion.EnableStatus(false);
                    motion.Hide();
                }
            }
        }

        public HomeState HomingState
        {
            get
            {
                return motion.home_state;
            }
        }

        public int HomingAxis
        {
            get
            {
                return motion.homing_axis;
            }
        }

        public int Number
        {
            get
            {
                return robot;
            }
        }

        public MotionState MoveState()
        {
            int axis;
            MotionState state;
            MotionState return_state = MotionState.Done;
            // Any axis error -> Error
            // If there is no error:
            // All axes done -> Done
            // Any axis moving -> Moving

            for (axis = 0; axis < ROBOT_AXES; axis++)
            {
                state = motion.MotionState(axis);
                switch (state)
                {
                    case MotionState.Done:
                        break;
                    case MotionState.Error:
                        return MotionState.Error;
                    case MotionState.Moving:
                        return_state = MotionState.Moving;
                        break;
                }
            }
            return return_state;
        }

        public MotionState MoveState(int axis)
        {
            return motion.MotionState(axis);
        }

        public AxisHomeState AxisHomingState(int axis)
        {
            return motion.home_axis_state[axis];
        }
        public void LocalPosition(double[] local)
        {
            motion.LocalPosition(local);
        }
        public void LocalCommandPosition(double[] local)
        {
            motion.LocalCommandPosition(local);
        }
        public void WorldPosition(out WorldCoords world)
        {
            motion.WorldPosition(out world);
        }

        public void LocalToWorld(double[] local, out Robot.WorldCoords world)
        {
            motion.LocalToWorld(local, out world);
        }
        public void WorldToLocal(Robot.WorldCoords world, double[] local)
        {
            motion.WorldToLocal(world, local);
        }

        public void StartPath(double x, double y, double velocity, double acceleration)
        {
            motion.StartPath(x, y, velocity, acceleration);
        }

        public void StartPath(double x, double y, double z, double velocity, double acceleration)
        {
            motion.StartPath(x, y, z, velocity, acceleration);
        }

        public void FinishPath(out double[] x, out double[] y, out uint count)
        {
            motion.FinishPath(out x, out y, out count);
        }

        public void FinishPath(out double[] x, out double[] y, out double[] z, out uint count)
        {
            motion.FinishPath(out x, out y, out z, out count);
        }

        public void AddPoint(double x, double y)
        {
            motion.AddPoint(x, y);
        }

        public void AddPoint(double x, double y, double z)
        {
            motion.AddPoint(x, y, z);
        }

        public void AddArc(double x, double y, double angle)
        {
            motion.AddArc(x, y, angle);
        }
        public void CutterOn(bool enable)
        {
            motion.CutterOn(enable);
        }

        #endregion Properties
    }
}


