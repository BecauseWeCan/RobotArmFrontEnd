using System;
using System.Runtime.InteropServices;
namespace SynqNetBot
{
    public interface IRobot
    {
        #region Methods---------------------------------------------------------------------------------
        void Initialize();
        void Config(String ConfigPath);
        void Home();
        void UseCurrentOrigins();
        void Move(double[] x, double speed, double accel);
        void Move(Robot.WorldCoords world, double speed, double accel);
        void MoveAxis(int axis, double x, double speed, double accel);
        void MovePVT(int axis, double[] position, double[] velocity, double[] time);
        void MoveBSpline(int axis, double[] position, double[] time);
        double MoveTime(int axis);
        void SetFeedRate(int axis, double FeedRate);
        double GetFeedRate(int axis);
        void ClearFault();
        void ClearFault(int axis);
        void Stop();
        void Stop(int axis);
        void EStop();
        void EStop(int axis);
        void Enable();
        void Enable(int axis);
        void Shutdown();
        void LocalPosition(double[] local);
        void LocalCommandPosition(double[] local);
        void WorldPosition(out Robot.WorldCoords world);
        void LocalToWorld(double[] local, out Robot.WorldCoords world);
        void WorldToLocal(Robot.WorldCoords world, double[] local);
        void CutterOn(bool enable);
        Robot.AxisHomeState AxisHomingState(int axis);
        Robot.MotionState MoveState();
        Robot.MotionState MoveState(int axis);

        
       #endregion Methods
        #region     Properties---------------------------------------------------------------------------------
        bool JoggingEnabled { get; set; }
        bool StatusEnabled { get; set; }
        Robot.HomeState HomingState { get; }
        int HomingAxis { get; }
        int Number { get; }
        
        #endregion  Properties
    }
}


