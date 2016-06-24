using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SynqNetBot
{
    public class Config
    {
        public void GetConfig(String path, String tag, out int value)
        {
            double dvalue;
            GetConfig(path, tag, out dvalue);
            value = (int)dvalue;
        }
        public void GetConfig(String path, String tag, out double value)
        {
            String pattern = "(?<=" + tag + "\\s+=\\s+)[+-.e\\d]+";

            StreamReader sr = new StreamReader(path);
            bool found = false;
            value = 0;
            while (sr.Peek() >= 0)
            {
                String line = sr.ReadLine();
                Regex r = new Regex(pattern);
                Match m = r.Match(line);
                if (m.Success)
                {
                    if (!found)
                    {
                        found = true;
                        value = double.Parse(m.Value);
                        Debug.Print("Configuration Item  \"" + tag + "\" = " + value.ToString(".000"));
                    }
                    else
                    {
                        Debug.Print("Configuration Item \"" + tag + "\" already found");
                    }
                }
            }
            if (!found)
            {
                // throw execption
                Debug.Print("Configuration Item \"" + tag + "\" not found");
            }
        }
    }
}
