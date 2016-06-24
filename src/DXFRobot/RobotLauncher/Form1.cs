using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SynqNetBot;
using System.Threading;

namespace DXFRobot
{
    public partial class Form1 : Form
    {
        private const String ConfigPath = "C:\\Projects\\Robot\\Config\\";
        private const String DXFPath = "C:\\Projects\\Robot\\DXF\\";

        private Robot robot0;
        private Robot robot1;
        private Form2 Robot0Form = new Form2();
        private Form2 Robot1Form = new Form2();

        public Form1()
        {
            InitializeComponent();
        }

        private void stage2_Load(object sender, EventArgs e)
        {
            robot0 = stage1.Robot(0);
            robot0.Config(ConfigPath + "Robot0\\");
            robot1 = stage1.Robot(1);
            robot1.Config(ConfigPath + "Robot1\\");
            Robot0Form.robot = robot0;
            Robot0Form.DXF.robot = robot0;
            Robot0Form.Text = "Robot 0";
            Robot0Form.Show();
            Robot1Form.robot = robot1;
            Robot1Form.DXF.robot = robot1;
            Robot1Form.Text = "Robot 1";
            Robot1Form.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            stage1.Reset();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stage1.Init();
            robot0.Init();
            robot1.Init();
        }
    }
}
