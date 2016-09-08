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

namespace RobotLauncher
{
    public partial class Form1 : Form
    {
        private const String ConfigPath = "C:\\Projects\\Robot\\Config\\";
        private const String VideoPath = "C:\\Projects\\Robot\\Video\\";

        private Robot robot0;
        private Robot robot1;
        private Form2 Robot0Form = new Form2();
        private Form2 Robot1Form = new Form2();
        private Form3 PlayerForm = new Form3();

        public Form1()
        {
            InitializeComponent();
        }

        private void stage1_Load(object sender, EventArgs e)
        {
            PlayerForm.Player1.settings.autoStart = false;
            robot0 = stage1.Robot(0);
            robot0.Config(ConfigPath + "Robot0\\");
            robot1 = stage1.Robot(1);
            robot1.Config(ConfigPath + "Robot1\\");
            Robot0Form.robot = robot0;
            Robot0Form.player = PlayerForm.Player1;
            Robot0Form.Text = "Robot 0";
            Robot0Form.Show();
            Robot1Form.robot = robot1;
            Robot1Form.player = PlayerForm.Player1;
            Robot1Form.Text = "Robot 1";
            comboBox1.SelectedIndex = 0;
            Robot1Form.Show();
            PlayerForm.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            stage1.Initialize();
            robot0.Initialize();
            robot1.Initialize();
            Robot0Form.EnableTimer(true);
            Robot1Form.EnableTimer(true);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            stage1.Reset();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Double MoveBeat;
            Double MoveDelay;
            String MoveMovie;
            String MoveFile;
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    MoveMovie = "Robot3.wmv";
                    MoveFile = "SusieQDance.txt";
                    MoveBeat = 0.935292;
                    MoveDelay = 1.0;
                    break;
                /*case 1:
                    DanceMovie = "SSLJ.mp3";
                    DanceFile = "SSLJDance.txt";
                    DanceBeat = 1.0;
                    DanceDelay = 0.87;
                    break;
                case 2:
                    DanceMovie = "FFYS_0001.wmv";
                    DanceFile = "FFYSDance.txt";
                    DanceBeat = 1.553289474;  // 152 beats starting at 17.5sec. (duration = 4min 13.6sec.) beat = (duration-delay)/number of beats aka sec/beat
                    DanceDelay = 0.413815789; // 17.5 sec - 11 beats.
                    break;*/
                default:
                    MoveMovie = "Robot3.wmv";
                    MoveFile = "SusieQDance.txt";
                    MoveBeat = 0.935292;
                    MoveDelay = 1.0;
                    break;
            }

            Debug.Print("Movement Movie = " + MoveMovie);
            PlayerForm.Player1.URL = VideoPath + MoveMovie;
            Robot0Form.MoveFile = ConfigPath + MoveFile;
            Robot0Form.MoveDelay = MoveDelay;
            Robot0Form.MoveBeat = MoveBeat;
            Robot1Form.MoveFile = ConfigPath + MoveFile;
            Robot1Form.MoveDelay = MoveDelay;
            Robot1Form.MoveBeat = MoveBeat;
            PlayerForm.Player1.settings.autoStart = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 4 && textBox1.Text.Substring(textBox1.Text.Length - 4).Equals(".txt"))
            {
                Robot0Form.MoveFile = ConfigPath + textBox1.Text;
                Robot0Form.MoveDelay = 1.0;
                Robot0Form.MoveBeat = 1.0;
                Robot1Form.MoveFile = ConfigPath + textBox1.Text;
                Robot1Form.MoveDelay = 1.0;
                Robot1Form.MoveBeat = 1.0;
                Robot0Form.LoadMovement();
                Robot1Form.LoadMovement();
                Thread.Sleep(1000);
                PlayerForm.Player1.close();
            }
            else
            {
                Robot0Form.LoadMovement();
                Robot1Form.LoadMovement();
                Thread.Sleep(1000);
                PlayerForm.Player1.Ctlcontrols.play();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Robot0Form.StopDance();
            Robot1Form.StopDance();
            PlayerForm.Player1.Ctlcontrols.stop();
        }
    }
}
