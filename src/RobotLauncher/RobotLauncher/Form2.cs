using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SynqNetBot;

namespace RobotLauncher
{
    public partial class Form2 : Form
    {
        public Robot robot;
        public double DanceDelay = 1.0;//seconds
        public string DanceFile = "C:\\Projects\\Robot\\Config\\SusieQDance.txt";
        public double DanceBeat = 0.935292;
        private double[] local = new double[Robot.ROBOT_AXES];
        private double[] old_local = new double[Robot.ROBOT_AXES];
        private double[] max_speed = new double[Robot.ROBOT_AXES];
        private double[] accel_time = new double[Robot.ROBOT_AXES];
        private double[] feed_rate = new double[Robot.ROBOT_AXES];
        private double xyz_speed;
        private double xyz_accel_time;
        private const double PVT_TIME_DELTA = 0.05; //seconds
        public double delay = 1.0;//seconds
        private Robot.WorldCoords world = new Robot.WorldCoords();
        private bool run_script = false;
        public AxWMPLib.AxWindowsMediaPlayer player;
        private double old_theta;
        private class PATH_Move
        {
            public double[] position;
        }
        private PATH_Move[] path_points = new PATH_Move[Robot.ROBOT_AXES];

        public Form2()
        {
            InitializeComponent();
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                max_speed[axis] = 320.0;
                accel_time[axis] = 0.5;
            }
            max_speed[0] = 90.0;
            max_speed[1] = 90.0;
            max_speed[2] = 90.0;
            xyz_speed = 100.0;
            xyz_accel_time = 0.5;

            int index;
            for (index = 0; index < Robot.ROBOT_AXES; index++)
            {
                local[index] = 0.0;
            }
            local[1] = -20.0;
            local[2] = 40.0;
            local[4] = -40.0;
            textBox1.Text = local[0].ToString("0.00");
            textBox2.Text = local[1].ToString("0.00");
            textBox3.Text = local[2].ToString("0.00");
            textBox4.Text = local[3].ToString("0.00");
            textBox5.Text = local[4].ToString("0.00");
            textBox6.Text = local[5].ToString("0.00");

            textBox9.Text = world.x.ToString("0.00");
            textBox8.Text = world.y.ToString("0.00");
            textBox7.Text = world.z.ToString("0.00");
            textBox29.Text = world.u.ToString("0.00");
            textBox28.Text = world.v.ToString("0.00");
            textBox30.Text = world.w.ToString("0.00");
            old_theta = 0.0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 1.0);
            }
            robot.Home();
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 1.0);
            }
            robot.JoggingEnabled = checkBox1.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            robot.StatusEnabled = checkBox3.Checked;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            local[0] = Convert.ToDouble(textBox1.Text);
            local[1] = Convert.ToDouble(textBox2.Text);
            local[2] = Convert.ToDouble(textBox3.Text);
            local[3] = Convert.ToDouble(textBox4.Text);
            local[4] = Convert.ToDouble(textBox5.Text);
            local[5] = Convert.ToDouble(textBox6.Text);
            bool[] enable = new bool[Robot.ROBOT_AXES];
            enable[0] = radioButton1.Checked;
            enable[1] = radioButton2.Checked;
            enable[2] = radioButton3.Checked;
            enable[3] = radioButton4.Checked;
            enable[4] = radioButton5.Checked;
            enable[5] = radioButton6.Checked;
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                double speed = max_speed[axis] * trackBar1.Value / 100.0;
                double accel = speed / accel_time[axis];
                if (enable[axis])
                {
                    robot.SetFeedRate(axis, 1.0);
                    robot.MoveAxis(axis, local[axis], speed, accel);
                }
            }
            textBox1.Text = local[0].ToString("0.00");
            textBox2.Text = local[1].ToString("0.00");
            textBox3.Text = local[2].ToString("0.00");
            textBox4.Text = local[3].ToString("0.00");
            textBox5.Text = local[4].ToString("0.00");
            textBox6.Text = local[5].ToString("0.00");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            local[0] = Convert.ToDouble(textBox1.Text);
            local[1] = Convert.ToDouble(textBox2.Text);
            local[2] = Convert.ToDouble(textBox3.Text);
            local[3] = Convert.ToDouble(textBox4.Text);
            local[4] = Convert.ToDouble(textBox5.Text);
            local[5] = Convert.ToDouble(textBox6.Text);
            double speed = max_speed[0] * trackBar1.Value / 100.0;
            double accel = speed / accel_time[0];
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 1.0);
            }
            robot.Move(local, speed, accel);
            textBox1.Text = local[0].ToString("0.00");
            textBox2.Text = local[1].ToString("0.00");
            textBox3.Text = local[2].ToString("0.00");
            textBox4.Text = local[3].ToString("0.00");
            textBox5.Text = local[4].ToString("0.00");
            textBox6.Text = local[5].ToString("0.00");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            robot.ClearFault();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            robot.Enable();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            robot.Shutdown();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            world.x = Convert.ToDouble(textBox9.Text);
            world.y = Convert.ToDouble(textBox8.Text);
            world.z = Convert.ToDouble(textBox7.Text);
            world.u = Convert.ToDouble(textBox29.Text);
            world.v = Convert.ToDouble(textBox28.Text);
            world.w = Convert.ToDouble(textBox30.Text);
            double speed = xyz_speed * trackBar1.Value / 100.0;
            double accel = speed / xyz_accel_time;
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 1.0);
            }
            robot.Move(world, speed, accel);
            textBox9.Text = world.x.ToString("0.00");
            textBox8.Text = world.y.ToString("0.00");
            textBox7.Text = world.z.ToString("0.00");
            textBox29.Text = world.u.ToString("0.00");
            textBox28.Text = world.v.ToString("0.00");
            textBox30.Text = world.w.ToString("0.00");
        }
        private void MoveToSound()
        {
            double theta, speed, accel;
            try
            {
                StreamReader sr = new StreamReader("C:\\Projects\\Robot\\SoundLocation.txt");
                string x_string = sr.ReadLine();
                string y_string = sr.ReadLine();
                sr.Close();
                if (robot.Number == 0)
                {
                    theta = Convert.ToDouble(x_string);
                }
                else
                {
                    theta = Convert.ToDouble(y_string);
                }
                local[0] = theta;
                textBox31.Text = theta.ToString();
                speed = max_speed[5] * trackBar1.Value / 100.0;
                accel = speed / (3.0 *accel_time[5]);
                double delta_theta = Math.Abs(theta - old_theta);
                if (delta_theta > 1.0)
                {
                    old_theta = theta;
                    //if (delta_theta < 2.0) // fix this later
                    //{
                    //    local[5] = 30.0;
                    //}
                    //else
                    //{
                    //    local[5] = 0.0;
                    //}
                    //robot.MoveAxis(5, local[5], speed, accel);
                    speed = max_speed[0] * trackBar1.Value / 100.0;
                    accel = speed / accel_time[0];
                    robot.MoveAxis(0, local[0], speed, accel);
                }
            }
            // Catch the IOException generated if the 
            // specified part of the file is locked.
            catch (IOException e)
            {
                Debug.Print("IO Exception:" + e.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            double[] local = new double[Robot.ROBOT_AXES];
            robot.LocalPosition(local);
            textBox15.Text = local[0].ToString("0.00");
            textBox14.Text = local[1].ToString("0.00");
            textBox13.Text = local[2].ToString("0.00");
            textBox12.Text = local[3].ToString("0.00");
            textBox11.Text = local[4].ToString("0.00");
            textBox10.Text = local[5].ToString("0.00");
            Robot.WorldCoords world = new Robot.WorldCoords();
            robot.WorldPosition(out world);
            textBox21.Text = world.x.ToString("0.00");
            textBox20.Text = world.y.ToString("0.00");
            textBox19.Text = world.z.ToString("0.00");
            textBox18.Text = world.u.ToString("0.00");
            textBox17.Text = world.v.ToString("0.00");
            textBox16.Text = world.w.ToString("0.00");
            Robot.HomeState hs = new Robot.HomeState();
            hs = robot.HomingState;
            textBox22.Text = hs.ToString();
            textBox23.Text = "";
            Robot.AxisHomeState ax_hs = new Robot.AxisHomeState();
            switch (hs)
            {
                case Robot.HomeState.Begin:
                case Robot.HomeState.Error:
                case Robot.HomeState.Homed:
                case Robot.HomeState.Unhomed:
                    break;
                case Robot.HomeState.Homing:
                    int axis = robot.HomingAxis;
                    ax_hs = robot.AxisHomingState(axis);
                    textBox23.Text = ax_hs.ToString();
                    break;
            }
            if (run_script)
            {
                double cp = player.Ctlcontrols.currentPosition;
                textBox26.Text = cp.ToString();
                double max_err = 0.0;
                if (cp > DanceDelay)
                {
                    if (!player.fullScreen) player.fullScreen = true;
                    int axis;
                    for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                    {
                        double mt = robot.MoveTime(axis);
                        double err = (cp - DanceDelay) - mt;
                        if (Math.Abs(err) > max_err) max_err = Math.Abs(err);
                        if (err > 0.01)
                        {
                            feed_rate[axis] += 0.01;
                        }
                        else if (err < -0.05)
                        {
                            feed_rate[axis] -= 0.01;
                        }
                        else
                        {
                            feed_rate[axis] = 1.0;
                        }
                        robot.SetFeedRate(axis, feed_rate[axis]);
                    }
                }
                Robot.MotionState m = new Robot.MotionState();
                m = robot.MoveState();
                textBox24.Text = m.ToString();
                textBox25.Text = max_err.ToString();

                switch (m)
                {
                    case Robot.MotionState.Done:
                        break;
                    case Robot.MotionState.Error:
                        run_script = false;
                        StopDance();
                        break;
                    case Robot.MotionState.Moving:
                        break;
                }
            }
            if (checkBox2.Checked)
            {
                MoveToSound();
            }
        }
        public void EnableTimer(bool enable)
        {
            timer1.Enabled = enable;
        }

        public void LoadDance()
        {
            robot.LocalCommandPosition(old_local);
            StreamReader sr = new StreamReader(DanceFile);
            double speed = trackBar1.Value * max_speed[0] / 100.0;
            double range = trackBar2.Value / 200.0;
            double beat = DanceBeat;
            double total_time = player.currentMedia.duration;
            int total_beats = (int)(total_time / beat + 0.5);
            int ticks_per_beat = (int)(beat / PVT_TIME_DELTA + 0.5);
            int total_ticks = total_beats * ticks_per_beat;
            double pvt_time_delta = beat / ticks_per_beat;
            Debug.Print("total_ticks = " + total_ticks);
            textBox27.Text = DanceBeat.ToString();
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                path_points[axis] = new PATH_Move();
                path_points[axis].position = new double[total_ticks + 1];
            }
            double[] time = new double[total_ticks + 1];
            int index;
            for (index = 0; index < total_ticks + 1; index++)
            {
                time[index] = pvt_time_delta;
            }
            index = 0;
            bool move_ok = true;
            while (sr.Peek() >= 0)
            {
                String line = sr.ReadLine();
                Debug.Print(line);
                int start = 0;
                int i = line.IndexOf(',', start);
                int move_type = 0;
                int beats = 0;
                int divisor = 1;
                if (i >= 0)
                {
                    move_type = int.Parse(line.Substring(start, i - start));
                    start = i + 1;
                    Debug.Print("Move Type = " + move_type);
                    i = line.IndexOf(',', start);
                    beats = int.Parse(line.Substring(start, i - start));
                    start = i + 1;
                    Debug.Print("Beats = " + beats);
                    i = line.IndexOf(',', start);
                    divisor = int.Parse(line.Substring(start, i - start));
                    start = i + 1;
                    Debug.Print("Divisor = " + divisor);
                    for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                    {
                        i = line.IndexOf(',', start);
                        local[axis] = double.Parse(line.Substring(start, i - start));
                        start = i + 1;
                        Debug.Print("Position[" + axis + "] = " + local[axis]);
                    }
                }
                else
                {
                    move_ok = false;
                }
                if (move_ok)
                {
                   switch (move_type)
                    {
                        case 0:
                            {
                                double sum = 0.0;
                                double[] dx = new double[Robot.ROBOT_AXES];
                                if (robot.Number == 1)
                                {
                                    local[0] = -local[0];
                                }

                                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                {
                                    double d = local[axis] - old_local[axis];
                                    sum += d * d;
                                    dx[axis] = d;
                                }
                                double dist = Math.Sqrt(sum);
                                if (beats == 0) beats = 4;
                                double T = dist * beats / (4 *speed);
                                int n_beats = (int)(T / beat) + 1;
                                T = beat * n_beats;
                                int n_points = n_beats * ticks_per_beat;
                                if (n_points > 3)
                                {
                                    if (index + (n_points + 1) < total_ticks)
                                    {
                                        for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                        {

                                            int n;
                                            for (n = 0; n <= n_points; n++)
                                            {
                                                double tm1 = (3.14159265 * n) / n_points;
                                                double x1 = old_local[axis] + dx[axis] * (1.0 - Math.Cos(tm1)) / 2.0;
                                                path_points[axis].position[n + index] = x1;
                                            }
                                            old_local[axis] = local[axis];
                                        }
                                        index += n_points + 1;
                                        Debug.Print("index = " + index);
                                    }
                                }
                            }
                            break;
                        case 1:
                            {
                                double[] dx = new double[Robot.ROBOT_AXES];
                                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                {
                                    dx[axis] = local[axis];
                                }
                                int n_points = beats * ticks_per_beat * divisor;
                                if (n_points > 3)
                                {
                                    if (index + (n_points) < total_ticks)
                                    {
                                        for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                        {

                                            int n;
                                            for (n = 0; n < n_points; n++)
                                            {
                                                double tm2 = (3.14159265 * 2.0 * n) / ticks_per_beat;
                                                double x2;
                                                if (axis == 0)
                                                {
                                                    x2 = old_local[axis] + range * dx[axis] * (1.0 - Math.Cos(tm2));
                                                }
                                                else
                                                {
                                                    x2 = old_local[axis] + range * dx[axis] * (1.0 - Math.Cos(tm2 / divisor));
                                                }
                                                path_points[axis].position[n + index] = x2;
                                            }
                                        }
                                        index += n_points;
                                        Debug.Print("index = " + index);
                                    }
                                }
                            }
                            break;
                      case 2:
                            {
                                double[] dx = new double[Robot.ROBOT_AXES];
                                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                {
                                    dx[axis] = local[axis];
                                }
                                int n_points = beats * ticks_per_beat * divisor;
                                if (n_points > 3)
                                {
                                    if (index + (n_points) < total_ticks)
                                    {
                                        for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                        {

                                            int n;
                                            for (n = 0; n < n_points; n++)
                                            {
                                                double tm2 = (3.14159265 * 2.0 * n) / ticks_per_beat;
                                                double x2;
                                                int mirror_control = n % (2 * ticks_per_beat);
                                                double mirror = 1.0;
                                                if (mirror_control > ticks_per_beat) mirror = -1.0;

                                                if (axis == 0)
                                                {
                                                    x2 = old_local[axis] + mirror * range * dx[axis] * (1.0 - Math.Cos(tm2));
                                                }
                                                else
                                                {
                                                    x2 = old_local[axis] + range * dx[axis] * (1.0 - Math.Cos(tm2 / divisor));
                                                }
                                                path_points[axis].position[n + index] = x2;
                                            }
                                        }
                                        index += n_points;
                                        Debug.Print("index = " + index);
                                    }
                                }
                            }
                            break;
                      case 3:
                            {
                                double[] dx = new double[Robot.ROBOT_AXES];
                                if (robot.Number == 1)
                                {
                                    local[0] = -local[0];
                                }

                                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                {
                                    dx[axis] = local[axis] - old_local[axis];
                                }
                                if (beats == 0) beats = 4;
                                int n_points = beats * ticks_per_beat;
                                if (n_points > 3)
                                {
                                    if (index + (n_points + 1) < total_ticks)
                                    {
                                        for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                                        {

                                            int n;
                                            for (n = 0; n <= n_points; n++)
                                            {
                                                double tm1 = (3.14159265 * n) / n_points;
                                                double x1 = old_local[axis] + dx[axis] * (1.0 - Math.Cos(tm1)) / 2.0;
                                                path_points[axis].position[n + index] = x1;
                                            }
                                            old_local[axis] = local[axis];
                                        }
                                        index += n_points + 1;
                                        Debug.Print("index = " + index);
                                    }
                                }
                            }
                            break;
                      default:
                            break;
                    }
                }
            }
            sr.Close();

            while (index < total_ticks + 1)
            {
                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                {
                    path_points[axis].position[index] = old_local[axis];
                }
                index++;
            }

            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 0.0);
                robot.MoveBSpline(axis, path_points[axis].position, time);
                feed_rate[axis] = 1.0;
            }
            run_script = true;
        }

        public void StopDance()
        {
            int axis;
            run_script = false;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 1.0);
            }
            robot.Stop();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            robot.UseCurrentOrigins();
        }
    }
}
