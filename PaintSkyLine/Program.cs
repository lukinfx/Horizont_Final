using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaintSkyLine
{
    static class Program
    {
        public static double Normalize360(double angle)
        {
            var x = angle - Math.Floor(angle / 360) * 360;
            return x;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            double x;
            x = Normalize360(-725);
            x = Normalize360(-365);
            x = Normalize360(-5);
            x = Normalize360(5);
            x = Normalize360(+365);
            x = Normalize360(+725);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
