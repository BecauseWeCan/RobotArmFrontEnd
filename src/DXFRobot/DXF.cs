using System;
using System.Diagnostics;

namespace DXFRobot
{
    public class DXF
    {
        public DXF()
        {
        }
        public void Execute()
        {
            StreamReader sr = new StreamReader(DXFFile);
            while (sr.Peek() >= 0)
            {
                String line = sr.ReadLine();
                switch (line)
                {
                    case "ARC":
                        Debug.Print(line);
                        break;
                    case "CIRCLE":
                        Debug.Print(line);
                        break;
                    case "LINE":
                        Debug.Print(line);
                        break;
                    case "LWPOLYLINE":
                        Debug.Print(line);
                        break;
                    case "POLYLINE":
                        Debug.Print(line);
                        break;
                    case "TEXT":
                        Debug.Print(line);
                        break;
                    default:
                        break;

                }
            }
            sr.Close();
        }
    }
}

