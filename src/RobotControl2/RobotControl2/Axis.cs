using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SynqNetBot
{
    class Axis
    {

        public double local_max_position;
        public double local_min_position;
        public double raw_max_position;
        public double raw_min_position;
        public int home_type;
        public double home_position;
        public double home_error_limit;
        public double home_offset;
        public double home_speed;
        public double home_accel;
        public double jog_speed;
        public double jog_accel;
        public double max_speed;
        public double accel_time;
        public double error_limit;
        public double stop_time;
        public double estop_time;
        public double position_tolerance;
        public double Origin;
        public double DEGREES_PER_COUNT;
        public double COUNTS_PER_DEGREE;
        public double Kp;
        public double Ki;
        public double Kd;
        public double Kvff;
        public double Kaff;


        public Axis(String ConfigFileName)
        {
            Config config = new Config();
            config.GetConfig(ConfigFileName, "Maximum Position", out local_max_position);
            config.GetConfig(ConfigFileName, "Minimum Position", out local_min_position);
            config.GetConfig(ConfigFileName, "Error Limit", out error_limit);
            config.GetConfig(ConfigFileName, "Position Tolerance", out position_tolerance);
            config.GetConfig(ConfigFileName, "Maximum Speed", out max_speed);
            config.GetConfig(ConfigFileName, "Accel Time", out accel_time);
            config.GetConfig(ConfigFileName, "Home Type", out home_type);
            config.GetConfig(ConfigFileName, "Home Position", out home_position);
            config.GetConfig(ConfigFileName, "Home Error Limit", out home_error_limit);
            config.GetConfig(ConfigFileName, "Home Offset", out home_offset);
            config.GetConfig(ConfigFileName, "Home Speed", out home_speed);
            config.GetConfig(ConfigFileName, "Home Accel", out home_accel);
            config.GetConfig(ConfigFileName, "Jog Speed", out jog_speed);
            config.GetConfig(ConfigFileName, "Jog Accel", out jog_accel);
            config.GetConfig(ConfigFileName, "Counts Per Degree", out COUNTS_PER_DEGREE);
            config.GetConfig(ConfigFileName, "Stop Time", out stop_time);
            config.GetConfig(ConfigFileName, "EStop Time", out estop_time);
            config.GetConfig(ConfigFileName, "Kp", out Kp);
            config.GetConfig(ConfigFileName, "Ki", out Ki);
            config.GetConfig(ConfigFileName, "Kd", out Kd);
            config.GetConfig(ConfigFileName, "Kvff", out Kvff);
            config.GetConfig(ConfigFileName, "Kaff", out Kaff);

            if (COUNTS_PER_DEGREE != 0.0)
            {
                DEGREES_PER_COUNT = 1.0 / COUNTS_PER_DEGREE;
            }
            else
            {
                DEGREES_PER_COUNT = 0.0;
            }
        }
        public void RawToLocal(double raw, out double local)
        {
            local = (raw - Origin) * DEGREES_PER_COUNT;
        }

        public void LocalToRaw( double local, out double raw)
        {
            raw = local * COUNTS_PER_DEGREE + Origin;
        }
    }
}
