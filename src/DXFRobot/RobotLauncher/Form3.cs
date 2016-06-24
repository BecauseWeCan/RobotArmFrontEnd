using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DXFRobot
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            pictureBox1.Width = Convert.ToInt32(pictureBox1.Height * DXFClass.field_width / DXFClass.field_height);
            this.Width = Convert.ToInt32(this.Height * DXFClass.field_width / DXFClass.field_height);
        }
    }
}
