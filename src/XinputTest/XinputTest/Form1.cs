using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using SupportLib;

namespace XinputTest
{
    public partial class Form1 : Form
    {
        public XBoxController xbox_controller;
        public Form1()
        {
            InitializeComponent();
            xbox_controller = new XBoxController(0);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(xbox_controller.IsConnected())
            {
                int packet = xbox_controller.PacketNumber();
                textBox1.Text = packet.ToString();

                if(xbox_controller.Button(SupportLib.Button.A)) 
                {
                    checkBox1.Checked = true;
                }
                else
                {
                    checkBox1.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.B))
                {
                    checkBox2.Checked = true;
                }
                else
                {
                    checkBox2.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.X))
                {
                    checkBox3.Checked = true;
                }
                else
                {
                    checkBox3.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.Y))
                {
                    checkBox4.Checked = true;
                }
                else
                {
                    checkBox4.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.DPAD_UP))
                {
                    checkBox5.Checked = true;
                }
                else
                {
                    checkBox5.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.DPAD_DOWN))
                {
                    checkBox6.Checked = true;
                }
                else
                {
                    checkBox6.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.DPAD_LEFT))
                {
                    checkBox7.Checked = true;
                }
                else
                {
                    checkBox7.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.DPAD_RIGHT))
                {
                    checkBox8.Checked = true;
                }
                else
                {
                    checkBox8.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.START))
                {
                    checkBox9.Checked = true;
                }
                else
                {
                    checkBox9.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.BACK))
                {
                    checkBox10.Checked = true;
                }
                else
                {
                    checkBox10.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.LEFT_THUMB))
                {
                    checkBox11.Checked = true;
                }
                else
                {
                    checkBox11.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.RIGHT_THUMB))
                {
                    checkBox12.Checked = true;
                }
                else
                {
                    checkBox12.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.LEFT_SHOULDER))
                {
                    checkBox13.Checked = true;
                }
                else
                {
                    checkBox13.Checked = false;
                }
                if (xbox_controller.Button(SupportLib.Button.RIGHT_SHOULDER))
                {
                    checkBox14.Checked = true;
                }
                else
                {
                    checkBox14.Checked = false;
                }
                progressBar1.Value = (int)(50.0 + xbox_controller.Thumbstick(SupportLib.Thumbstick.RIGHT_X) * 50.0);
                progressBar2.Value = (int)(50.0 + xbox_controller.Thumbstick(SupportLib.Thumbstick.RIGHT_Y) * 50.0);
                progressBar3.Value = (int)(50.0 + xbox_controller.Thumbstick(SupportLib.Thumbstick.LEFT_X) * 50.0);
                progressBar4.Value = (int)(50.0 + xbox_controller.Thumbstick(SupportLib.Thumbstick.LEFT_Y) * 50.0);
                progressBar5.Value = (int)(xbox_controller.Trigger(SupportLib.Trigger.LEFT) * 100.0);
                progressBar6.Value = (int)(xbox_controller.Trigger(SupportLib.Trigger.RIGHT) * 100.0);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (xbox_controller.IsConnected())
            {
                button1.BackColor = Color.Green;
                timer1.Interval = 50;
                timer1.Enabled = true;
            }
            else
            {
                button1.BackColor = Color.Red;
            }
        }
    }
}
