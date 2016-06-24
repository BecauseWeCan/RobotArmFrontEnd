using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using SynqNetBot;

namespace DXFRobot
{
    public class DXFClass
    {
        public const double MM = 25.4;              // inches to mm
        public const double INCHES = (1.0 / MM);    // mm to inches
        public const double RADIANS = 0.017453293;
        public const double DEGREES = (1.0 / RADIANS);
        public const double x_origin = 700;
        public const double y_origin = -1900;
        public const double z_origin = 1058;
        public const double u_origin = 0.0;
        public const double v_origin = -90.0;
        public const double w_origin = 0.0;
        public const double field_width = 600;
        public const double field_height = 1000;

        public Robot robot;
        public bool layer_done;
        public bool all_done;
        public double cut_z;
        public double slew_z;
        public double[] cut_path_x;
        public double[] cut_path_y;
        public double[] cut_path_z;
        public double[] slew_path_x;
        public double[] slew_path_y;
        public double[] slew_path_z;
        public double[] end_path_x;
        public double[] end_path_y;
        public double[] end_path_z;
        public int cut_point_count;
        public int slew_point_count;
        public int end_point_count;
        public double start_x;
        public double start_y;
        public double start_z;
        public double cut_speed;
        public double cut_accel;
        public double slew_speed;
        public double slew_accel;
        public string element;
        private int next_layer;

        private double plot_x_scale;
        private double plot_y_scale;
        private double plot_x_origin;
        private double plot_y_origin;
        public Form3 DXFForm = new Form3();
        private int layer;
        public  int current_layer;
        private string line_type;
        private int text_style;
        private int cut_color;
        private int vertex_flag;
        private int pline_type;
        private int text_justification;
        private double x0;
        private double y0;
        private double x1;
        private double y1;
        private double x2;
        private double y2;
        private double t1;
        private double t2;
        private double rad;
        private double bulge_factor;
        private double last_bulge;
        private string text_string;
        private Bitmap Canvas;
        private Graphics Screen;
        private Pen pen;
        private StreamReader sr;
        public DXFClass()
        {
            Canvas = new Bitmap(DXFForm.pictureBox1.Width, DXFForm.pictureBox1.Height);
            Screen = Graphics.FromImage(Canvas);
            pen = new Pen(Color.Black);
            plot_x_scale = DXFForm.pictureBox1.Height / field_height;
            plot_y_scale = -plot_x_scale;
            plot_x_origin = 0;
            plot_y_origin = -field_height;
            cut_speed = 1.0;
            cut_accel = 10000.0;
            slew_speed = 30.0;
            slew_accel = 10000.0;
            cut_z = 0.0;
            slew_z = 50.0;
        }
        public void Init(int robot)
        {
            x0 = 0;
            x1 = 0;
            x2 = 0;
            y0 = 0;
            y1 = 0;
            y2 = 0;
            t1 = 0;
            t2 = 0;
            rad = 0;
            layer = 1;
            current_layer = 1;
            next_layer = 1;
            text_style = 0;
            bulge_factor = 0;
            last_bulge = 0;
            cut_color = 0;
            vertex_flag = 0;
            pline_type = 0;
            text_justification = 0;
            DXFForm.Text = "Robot " + robot + " DXF";
            DXFForm.Show();
            Screen.Clear(Color.WhiteSmoke);
            DXFForm.pictureBox1.Image = Canvas;

            layer_done = false;
            all_done = false;
        }
        public void OpenDXF(string DXFFile)
        {
            sr = new StreamReader(DXFFile);
            layer_done = false;
        }
        public bool Execute()
        {
            bool success;
            while (sr.Peek() >= 0)
            {
                String line = sr.ReadLine();
                success = false;
                switch (line)
                {
                    case "ARC":
                        Debug.Print(line);
                        success = do_arc();
                        break;
                    case "CIRCLE":
                        Debug.Print(line);
                        success = do_circle();
                        break;
                    case "LINE":
                        Debug.Print(line);
                        success = do_line();
                        break;
                    case "LWPOLYLINE":
                        Debug.Print(line);
                        break;
                    case "POLYLINE":
                        Debug.Print(line);
                        success = do_poly();
                        break;
                    case "TEXT":
                        Debug.Print(line);
                        break;
                    default:
                        break;

                }
                if (success) return success;
            }
            sr.Close();
            layer_done = true;
            if (next_layer > current_layer)
            {
                current_layer = next_layer;
            }
            else
            {
                all_done = true;
            }
            return false;
        }

        public void WorldtoDXF(Robot.WorldCoords world,  out Robot.WorldCoords dxf)
        {
            dxf.x = world.y - y_origin;
            dxf.y = -(world.x - x_origin);
            dxf.z = world.z - z_origin;
            dxf.u = world.u - u_origin;
            dxf.v = world.v - v_origin;
            dxf.w = world.w - w_origin;
        }

        public void DXFtoWorld(Robot.WorldCoords dxf, out Robot.WorldCoords world)
        {
            world.x = -(dxf.y - x_origin);
            world.y = dxf.x + y_origin;
            world.z = dxf.z + z_origin;
            world.u = dxf.u + u_origin;
            world.v = dxf.v + v_origin;
            world.w = dxf.w + w_origin;
        }

        private bool get_param()
        {
            int param_type = 0;
            bool success;
            success = false;

            string line = sr.ReadLine();
            //Debug.Print(line);
            param_type = Convert.ToInt32(line);
            switch (param_type)
            {
                case 0:
                    success = true;
                    break;
                case 6:
                    line_type = sr.ReadLine();
                    break;
                case 1:
                    text_string = sr.ReadLine();
                    break;
                case 7:
                    line = sr.ReadLine();
                    text_style = Convert.ToInt32(line);
                    break;
                case 8:
                    line = sr.ReadLine();
                    layer = int.Parse(line);
                    if (layer != current_layer) success = true;
                    if (layer > current_layer)
                    {
                        if (next_layer == current_layer) next_layer = layer;
                        if (layer < next_layer) next_layer = layer;
                    }
                    break;
                case 10:
                    line = sr.ReadLine();
                    x0 = Convert.ToDouble(line) * MM;
                    break;
                case 11:
                    line = sr.ReadLine();
                    x1 = Convert.ToDouble(line) * MM;
                    break;
                case 20:
                    line = sr.ReadLine();
                    y0 = Convert.ToDouble(line) * MM;
                    break;
                case 21:
                    line = sr.ReadLine();
                    y1 = Convert.ToDouble(line) * MM;
                    break;
                case 40:
                    line = sr.ReadLine();
                    rad = Convert.ToDouble(line) * MM;
                    break;
                case 41:
                    line = sr.ReadLine();
                    break;
                case 42:
                    line = sr.ReadLine();
                    bulge_factor = Convert.ToDouble(line);
                    break;
                case 50:
                    line = sr.ReadLine();
                    t1 = Convert.ToDouble(line);
                    break;
                case 51:
                    line = sr.ReadLine();
                    t2 = Convert.ToDouble(line);
                    break;
                case 62:
                    line = sr.ReadLine();
                    cut_color = Convert.ToInt32(line);
                    break;
                case 66:
                    line = sr.ReadLine();
                    vertex_flag = Convert.ToInt32(line);
                    break;
                case 70:
                    line = sr.ReadLine();
                    pline_type = Convert.ToInt32(line);
                    break;
                case 72:
                    line = sr.ReadLine();
                    text_justification = Convert.ToInt32(line);
                    break;
                case 5:
                case 30:
                case 31:
                case 73:
                    line = sr.ReadLine();
                    break;
               
                default:
                    line = sr.ReadLine();
                    break;
            }
            return success;
        }
        private void start_point(double x, double y)
        {
            robot.StartPath(x, y, cut_speed, cut_accel);
            x2 = x;
            y2 = y;
            last_bulge = bulge_factor;
        }

        private void store_point(double x, double y)
        {
            robot.AddPoint(x, y);
            plot_line(pen, x2, y2, x, y);
            x2 = x;
            y2 = y;
            last_bulge = bulge_factor;
        }

        private void end_point()
        {
            robot.FinishPath(out cut_path_x, out cut_path_y, out cut_point_count);
            if (cut_point_count > 0)
            {
                int i;
                cut_path_z = new double[cut_point_count];
                for (i = 0; i < cut_point_count; i++)
                {
                    cut_path_z[i] = cut_z;
                }
            }
            robot.StartPath(start_x, start_y, start_z, slew_speed, slew_accel);
            robot.AddPoint(cut_path_x[0], cut_path_y[0], slew_z);
            robot.AddPoint(cut_path_x[0], cut_path_y[0], cut_z);
            robot.FinishPath(out slew_path_x, out slew_path_y, out slew_path_z, out slew_point_count);
            start_x = cut_path_x[cut_point_count - 1];
            start_y = cut_path_y[cut_point_count - 1];
            start_z = cut_path_z[cut_point_count - 1];
            robot.StartPath(start_x, start_y, start_z, slew_speed, slew_accel);
            robot.AddPoint(start_x, start_y, slew_z);
            robot.FinishPath(out end_path_x, out end_path_y, out end_path_z, out end_point_count);
            start_x = end_path_x[end_point_count - 1];
            start_y = end_path_y[end_point_count - 1];
            start_z = end_path_z[end_point_count - 1];
        }
        public void park()
        {
            cut_point_count = 0;
            slew_point_count = 0;
            robot.StartPath(start_x, start_y, start_z, slew_speed, slew_accel);
            robot.AddPoint(0, 0, slew_z);
            robot.FinishPath(out end_path_x, out end_path_y, out end_path_z, out end_point_count);
            start_x = end_path_x[end_point_count - 1];
            start_y = end_path_y[end_point_count - 1];
            start_z = end_path_z[end_point_count - 1];
        }
        private bool do_arc()
        {
            float xp1;
            float yp1;
            float rp;
            double theta;
            float tp;
            float tp1;

            while (!get_param()) ;
            if (layer != current_layer) return false;
            xp1 = Convert.ToSingle((x0 + plot_x_origin) * plot_x_scale);
            yp1 = Convert.ToSingle((y0 + plot_y_origin) * plot_y_scale);
            rp = Convert.ToSingle(rad * plot_x_scale);
            tp1 = Convert.ToSingle(t1);
            theta = t2 - t1;
            if (theta < 0.0) theta = 360.0 + theta;
            tp = Convert.ToSingle(theta);
            Screen.DrawArc(pen, xp1 - rp, yp1 - rp, 2 * rp, 2 * rp, -tp1, -tp);
            DXFForm.pictureBox1.Image = Canvas;
            x1 = x0 + rad * Math.Cos(t1 * RADIANS);
            y1 = y0 + rad * Math.Sin(t1 * RADIANS);
            start_point(x1, y1);
            robot.AddArc(x0, y0, theta);
            end_point();
            x2 = x0 + rad * Math.Cos(t2 * RADIANS);
            y2 = y0 + rad * Math.Sin(t2 * RADIANS);
           return true;
        }
        private bool do_circle()
        {
            float xp1;
            float yp1;
            float rp;
            double dx;
            double dy;
            double dist;

            while (!get_param()) ;
            if (layer != current_layer) return false;
            xp1 = Convert.ToSingle((x0 + plot_x_origin) * plot_x_scale);
            yp1 = Convert.ToSingle((y0 + plot_y_origin) * plot_y_scale);
            rp = Convert.ToSingle(rad * plot_x_scale);
            Screen.DrawEllipse(pen, xp1 - rp, yp1 - rp, 2 * rp, 2 * rp);
            DXFForm.pictureBox1.Image = Canvas;
            dx = x0 - x2;
            dy = y0 - y2;
            dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > 0)
            {
                x1 = x0 - dx * rad / dist;
                y1 = y0 - dy * rad / dist;
                start_point(x1, y1);
                robot.AddArc(x0, y0, 360.0);
                end_point();
            }
            return true;
        }
        private bool do_line()
        {
            double d1;
            double d2;

            while (!get_param()) ;
            if (layer != current_layer) return false;
            d1 = (x2 - x0) * (x2 - x0) + (y2 - y0) * (y2 - y0);
            d2 = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
            if (d2 < d1)
            {
                d1 = x0;
                d2 = y0;
                x0 = x1;
                y0 = y1;
                x1 = d1;
                y1 = d2;
            }
            start_point(x0, y0);
            store_point(x1, y1);
            end_point();
            return true;
        }
        private bool get_vertex()
        {
            bool done = false;
            bool retval = false;
            while (!done)
            {

                String line = sr.ReadLine();
                switch (line)
                {
                    case "VERTEX":
                        bulge_factor = 0.0;
                        while (!get_param()) ;
                        done = true;
                        retval = true;
                        break;
                    case "SEQEND":
                        done = true;
                        break;
                    default:
                        break;

                }
            }
            return retval;
        }
        private void poly_point(double x, double y)
        {
            double thet;
            double a;
            double a2;
            double b;
            double r;
            double d;
            double x3;
            double y3;
            double xc;
            double yc;
            float xpc;
            float ypc;
            float xp1;
            float yp1;
            float xp2;
            float yp2;
            float rp;
            double m;
            double m2;
            double mx;
            double my;
            double dx;
            double dy;
            Pen pen2 = new Pen(Color.Green);

            if (last_bulge != 0.0)
            {
                dx = (x - x2) / 2;
                dy = (y - y2) / 2;
                a2 = dx * dx + dy * dy;
                a = Math.Sqrt(a2);
                b = last_bulge * a;
                d = (a2 - b * b) / (2 * b);
                r = b + d;
                if (r < 0.0) r = -r;
                x3 = (x + x2) / 2;
                y3 = (y + y2) / 2;
                if (dy != 0.0)
                {
                    m = dx / dy;
                    m2 = Math.Sqrt(1 + m * m);
                    mx = 1 / m2;
                    my = m * mx;
                    if ((dx < 0) && (dy < 0)) my = -my;
                    if ((dx < 0) && (dy > 0)) mx = -mx;
                    if ((dx > 0) && (dy > 0)) mx = -mx;
                    if ((dx > 0) && (dy < 0)) my = -my;
                    xc = x3 + d * mx;
                    yc = y3 + d * my;
                }
                else
                {
                    if (dx > 0)
                    {
                        yc = y3 + d;
                    }
                    else
                    {
                        yc = y3 - d;
                    }
                    xc = x3;
                }
                xpc = Convert.ToSingle((xc + plot_x_origin) * plot_x_scale);
                ypc = Convert.ToSingle((yc + plot_y_origin) * plot_y_scale);
                xp1 = Convert.ToSingle((x + plot_x_origin) * plot_x_scale);
                yp1 = Convert.ToSingle((y + plot_y_origin) * plot_y_scale);
                xp2 = Convert.ToSingle((x2 + plot_x_origin) * plot_x_scale);
                yp2 = Convert.ToSingle((y2 + plot_y_origin) * plot_y_scale);
                rp = Convert.ToSingle(r * plot_x_scale);
                Screen.DrawLine(pen2, xp1, yp1, xpc, ypc);
                pen2.Color = Color.Blue;
                Screen.DrawLine(pen2, xpc, ypc, xp2, yp2);
                pen2.Color = Color.Chartreuse;
                Screen.DrawLine(pen2, xp1, yp1, xp2, yp2);
                xp1 = Convert.ToSingle((x3 + plot_x_origin) * plot_x_scale);
                yp1 = Convert.ToSingle((y3 + plot_y_origin) * plot_y_scale);
                pen2.Color = Color.Black;
                Screen.DrawLine(pen2, xp1, yp1, xpc, ypc);
                Screen.DrawEllipse(pen2, xpc - rp, ypc - rp, 2 * rp, 2 * rp);
                DXFForm.pictureBox1.Image = Canvas;
                thet = 4 * Math.Atan(last_bulge) * DEGREES;
                robot.AddArc(xc, yc, thet);
                x2 = x;
                y2 = y;
            }
            else
            {
                store_point(x, y);
            }
            last_bulge = bulge_factor;
        }
        
        private bool do_poly()
        {
            double x_save;
            double y_save;
            while (!get_param()) ;
            if (layer != current_layer) return false;
            if(vertex_flag == 1)
            {
                get_vertex();
                start_point(x0, y0);
                x_save = x0;
                y_save = y0;
                while (get_vertex())
                {
                    poly_point(x0, y0);
                }
                if (pline_type == 1)
                {
                    poly_point(x_save, y_save);
                }
                end_point();
                return true;
            }            
            return false;
        }
        public void plot_line(Pen pen, double x0, double y0, double x1, double y1)
        {
            float xp1;
            float yp1;
            float xp2;
            float yp2;

            xp1 = Convert.ToSingle((x0 + plot_x_origin) * plot_x_scale);
            yp1 = Convert.ToSingle((y0 + plot_y_origin) * plot_y_scale);
            xp2 = Convert.ToSingle((x1 + plot_x_origin) * plot_x_scale);
            yp2 = Convert.ToSingle((y1 + plot_y_origin) * plot_y_scale);
            Screen.DrawLine(pen, xp1, yp1, xp2, yp2);
            DXFForm.pictureBox1.Image = Canvas;
        }
    }
}

