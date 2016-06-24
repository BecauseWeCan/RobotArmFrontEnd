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

namespace DXFRobot
{
    public partial class Form2 : Form
    {
        public Robot robot;
        public string DXFFile = "C:\\Projects\\Robot\\DXF\\Circles.dxf";
        public DXFClass DXF = new DXFClass();
        private double[] local = new double[Robot.ROBOT_AXES];
        private double[] old_local = new double[Robot.ROBOT_AXES];
        private double[] max_speed = new double[Robot.ROBOT_AXES];
        private double[] accel_time = new double[Robot.ROBOT_AXES];
        private double[] feed_rate = new double[Robot.ROBOT_AXES];
        private double xyz_speed;
        private double xyz_accel_time;
        private double cut_speed;
        private double slew_speed;
        private double old_x;
        private double old_y;
        private const double PVT_TIME_DELTA = 0.05; //seconds
        public double cutter_on_delay = 100.0;//millisec.
        private int ticks;
        private Robot.WorldCoords world = new Robot.WorldCoords();
        private Robot.WorldCoords dxf = new Robot.WorldCoords();
        private class PATH_Move
        {
            public double[] position;
        }
        private PATH_Move[] path_points = new PATH_Move[Robot.ROBOT_AXES];

        private enum CutState
        {
            Idle,
            Init,
            Start,
            Wait,
            Continue,
            SlewStart,
            Slew,
            CutterOn,
            CutStart,
            Cut,
            CutterOff,
            EndStart,
            End,
            Stop,
            EndStop,
            Done,
        }
        private CutState state;
        private int element_count;
        private Pen pen;
        private enum StartButtonState
        {
            Start,
            Running,
            Continue,
        }
        private StartButtonState sbs;

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
            cut_speed = 66.0;
            slew_speed = 500.0;


            int index;
            for (index = 0; index < Robot.ROBOT_AXES; index++)
            {
                local[index] = 0.0;
            }
            local[1] = -60.0;
            local[2] = 0.0;
            local[4] = 0.0;
            textBox1.Text = local[0].ToString("0.00");
            textBox2.Text = local[1].ToString("0.00");
            textBox3.Text = local[2].ToString("0.00");
            textBox4.Text = local[3].ToString("0.00");
            textBox5.Text = local[4].ToString("0.00");
            textBox6.Text = local[5].ToString("0.00");

            textBox9.Text = dxf.x.ToString("0.00");
            textBox8.Text = dxf.y.ToString("0.00");
            textBox7.Text = dxf.z.ToString("0.00");
            textBox29.Text = dxf.u.ToString("0.00");
            textBox28.Text = dxf.v.ToString("0.00");
            textBox30.Text = dxf.w.ToString("0.00");
            state = CutState.Idle;
            pen = new Pen(Color.Red);
            ticks = 0;
            sbs = StartButtonState.Start;
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
            dxf.x = Convert.ToDouble(textBox9.Text);
            dxf.y = Convert.ToDouble(textBox8.Text);
            dxf.z = Convert.ToDouble(textBox7.Text);
            dxf.u = Convert.ToDouble(textBox29.Text);
            dxf.v = Convert.ToDouble(textBox28.Text);
            dxf.w = Convert.ToDouble(textBox30.Text);
            double speed = xyz_speed * trackBar1.Value / 100.0;
            double accel = speed / xyz_accel_time;
            int axis;
            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.SetFeedRate(axis, 1.0);
            }
            DXF.DXFtoWorld(dxf, out world);
            robot.Move(world, speed, accel);
            DXF.WorldtoDXF(world, out dxf);
            textBox9.Text = dxf.x.ToString("0.00");
            textBox8.Text = dxf.y.ToString("0.00");
            textBox7.Text = dxf.z.ToString("0.00");
            textBox29.Text = dxf.u.ToString("0.00");
            textBox28.Text = dxf.v.ToString("0.00");
            textBox30.Text = dxf.w.ToString("0.00");
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
            Robot.WorldCoords dxf = new Robot.WorldCoords();
            robot.WorldPosition(out world);
            DXF.WorldtoDXF(world, out dxf);
            textBox21.Text = dxf.x.ToString("0.00");
            textBox20.Text = dxf.y.ToString("0.00");
            textBox19.Text = dxf.z.ToString("0.00");
            textBox18.Text = dxf.u.ToString("0.00");
            textBox17.Text = dxf.v.ToString("0.00");
            textBox16.Text = dxf.w.ToString("0.00");
            Robot.HomeState hs = new Robot.HomeState();
            hs = robot.HomingState;
            textBox22.Text = hs.ToString();
            textBox23.Text = "";
            textBox26.Text = DXF.element;
            textBox27.Text = DXF.current_layer.ToString();
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

            Robot.MotionState m = new Robot.MotionState();
            m = robot.MoveState();
            switch (state)
            {
                case CutState.Idle:
                    sbs = StartButtonState.Start;
                    break;
                case CutState.Init:
                    double speed = cut_speed * trackBar1.Value / 100.0;
                    double accel = speed / xyz_accel_time;
                    DXF.cut_speed = speed;
                    DXF.cut_accel = accel;
                    speed = slew_speed * trackBar1.Value / 100.0;
                    accel = speed / xyz_accel_time;
                    DXF.slew_speed = speed;
                    DXF.slew_accel = accel;
                    DXF.Init(robot.Number);
                    DXF.start_x = dxf.x;
                    DXF.start_y = dxf.y;
                    DXF.start_z = dxf.z;
                    state = CutState.Start;
                    break;
                case CutState.Wait:
                    sbs = StartButtonState.Continue;
                    break;
                case CutState.Start:
                    DXF.OpenDXF(DXFFile);
                    element_count = 0;
                    state = CutState.Continue;
                    sbs = StartButtonState.Running;
                    break;
                case CutState.Continue:
                    bool success = DXF.Execute();
                    if (success)
                    {
                        Debug.Print("Success!");
                        Debug.Print("Cutting Element " + element_count);
                        Debug.Print("Slew Point Count = " + DXF.slew_point_count);
                        Debug.Print("Cut Point Count = " + DXF.cut_point_count);
                        Debug.Print("End Point Count = " + DXF.end_point_count);
                        element_count++;
                        state = CutState.SlewStart;
                    }
                    else
                    {
                        DXF.park();
                        state = CutState.EndStart;
                    }
                    break;
                case CutState.SlewStart:
                    CutElement(DXF.slew_path_x, DXF.slew_path_y, DXF.slew_path_z, DXF.slew_point_count);
                    state = CutState.Slew;
                    break;
                case CutState.Slew:
                    switch (m)
                    {
                        case Robot.MotionState.Done:
                            robot.CutterOn(true);
                            ticks = Convert.ToInt32(cutter_on_delay / timer1.Interval) + 1;
                            state = CutState.CutterOn;
                            break;
                        case Robot.MotionState.Error:
                            robot.CutterOn(false);
                            state = CutState.Done;
                            break;
                        case Robot.MotionState.Moving:
                            break;
                    }
                    break;
                case CutState.CutterOn:
                    if (ticks > 0)
                    {
                        ticks--;
                    }
                    else
                    {
                     state = CutState.CutStart;
                    }
                    break;
                case CutState.CutStart:
                    CutElement(DXF.cut_path_x, DXF.cut_path_y, DXF.cut_path_z, DXF.cut_point_count);
                    int i;
                    for (i = 0; i < DXF.cut_point_count; i++)
                    {
                        Debug.Print(DXF.cut_path_x[i] + "," + DXF.cut_path_y[i]);
                    }
                    state = CutState.Cut;
                    old_x = DXF.cut_path_x[0];
                    old_y = DXF.cut_path_y[0];
                    break;
                case CutState.Cut:
                    switch (m)
                    {
                        case Robot.MotionState.Done:
                            state = CutState.CutterOff;
                            break;
                        case Robot.MotionState.Error:
                            robot.CutterOn(false);
                            Stop();
                            state = CutState.Done;
                            break;
                        case Robot.MotionState.Moving:
                            DXF.plot_line(pen, old_x, old_y, dxf.x, dxf.y);
                            old_x = dxf.x;
                            old_y = dxf.y;
                            break;
                    }
                    break;
                case CutState.CutterOff:
                    robot.CutterOn(false);
                    state = CutState.EndStart;
                    break;
                case CutState.EndStart:
                    CutElement(DXF.end_path_x, DXF.end_path_y, DXF.end_path_z, DXF.end_point_count);
                    state = CutState.End;
                    break;
                case CutState.End:
                    switch (m)
                    {
                        case Robot.MotionState.Done:
                            if (DXF.all_done)
                            {
                                state = CutState.Done;
                            }
                            else if (DXF.layer_done)
                            {
                                state = CutState.Wait;
                            }
                            else
                            {
                                state = CutState.Continue;
                            }
                            break;
                        case Robot.MotionState.Error:
                            robot.CutterOn(false);
                            Stop();
                            state = CutState.Done;
                            break;
                        case Robot.MotionState.Moving:
                            break;
                    }
                    break;
                case CutState.Stop:
                    robot.CutterOn(false);
                    Stop();
                    state = CutState.EndStop;
                    break;
                case CutState.EndStop:
                    switch (m)
                    {
                        case Robot.MotionState.Done:
                            DXF.start_x = dxf.x;
                            DXF.start_y = dxf.y;
                            DXF.start_z = dxf.z;
                            DXF.park();
                            state = CutState.EndStart;
                            break;
                        case Robot.MotionState.Error:
                            state = CutState.Done;
                            break;
                        case Robot.MotionState.Moving:
                            break;
                    }
                    break;
                case CutState.Done:
                    sbs = StartButtonState.Start;
                    break;
            }
            switch (sbs)
            {
                case StartButtonState.Start:
                    button9.BackColor = Color.Green;
                    button9.Text = "Start";
                    break;
                case StartButtonState.Running:
                    button9.BackColor = Color.Gray;
                    button9.Text = "Running";
                    break;
                case StartButtonState.Continue:
                    button9.BackColor = Color.Blue;
                    button9.Text = "Continue";
                    break;
            }
        }

        public void Stop()
        {
            DXF.all_done = true;
            DXF.layer_done = true;
            robot.Stop();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            robot.UseCurrentOrigins();
        }

        private void CutElement(double[] x, double[] y, double[] z, int count)
        {
            int axis;
            Robot.WorldCoords world = new Robot.WorldCoords();
            Robot.WorldCoords dxf = new Robot.WorldCoords();
            robot.WorldPosition(out world);
            DXF.WorldtoDXF(world, out dxf);

            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                path_points[axis] = new PATH_Move();
                path_points[axis].position = new double[count];
            }
            double[] time = new double[count];
            int index;
            double[] local = new double[Robot.ROBOT_AXES];
            for (index = 0; index < count; index++)
            {
                time[index] = 0.05;
                dxf.x = x[index];
                dxf.y = y[index];
                dxf.z = z[index];
                DXF.DXFtoWorld(dxf, out world);
                robot.WorldToLocal(world, local);
                for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
                {
                    path_points[axis].position[index] = local[axis];
                }
            }

            for (axis = 0; axis < Robot.ROBOT_AXES; axis++)
            {
                robot.MoveBSpline(axis, path_points[axis].position, time);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                DXFFile = openFileDialog1.FileName;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            switch (sbs)
            {
                case StartButtonState.Start:
                    state = CutState.Init;
                    break;
                case StartButtonState.Running:
                    break;
                case StartButtonState.Continue:
                    state = CutState.Start;
                    break;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            state = CutState.Stop;
        }
    }
}
