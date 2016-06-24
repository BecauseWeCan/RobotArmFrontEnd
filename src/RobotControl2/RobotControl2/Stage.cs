using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using SynqNetBot;
using SupportLib;

namespace RobotControl
{
    public partial class Stage : UserControl
    {
        private Robot[] robot = new Robot[ROBOTS];
        public const int ROBOTS = 2;

        public Stage()
        {
            InitializeComponent();
            robot[0] = new Robot(0);
//            robot[0].controller = controller1;
            robot[1] = new Robot(1);
//            robot[1].controller = controller1;
            Config();
        }

        public void Reset()
        {
 //           controller1.Reset();
            Config();
        }

        public void Config()
        {
            //controller1.AxisCount = ROBOTS * SynqNetBot.Robot.ROBOT_AXES;
            //controller1.MotionCount = ROBOTS * SynqNetBot.Robot.ROBOT_AXES;
            //controller1.CaptureCount = ROBOTS * SynqNetBot.Robot.ROBOT_AXES;
        }
        public void Initialize()
        {
        }

        public Robot Robot(int index)
        {
            if((index < 0) || (index >= ROBOTS))
            {
                return null;
            }
            else
            {
                return robot[index];
            }
        }
    }
}
