using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Device.Location;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Peaks360Lib.Domain.Models;

namespace PaintSkyLine
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var lat = double.Parse(textBoxLat.Text.Replace(",","."), CultureInfo.InvariantCulture);
            var lon = double.Parse(textBoxLon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            var alt = double.Parse(textBoxAlt.Text, CultureInfo.InvariantCulture);
            var visibility = int.Parse(textBoxVisibility.Text);
            var minDist = int.Parse(textBoxMinDist.Text);
            skyLine1.SetMyLocation(new GpsLocation(lon, lat, alt));
            skyLine1.SetMinDist(minDist);
            skyLine1.SetVisibility(visibility);

            var start = Environment.TickCount;
            skyLine1.CalculateProfile();
            var end = Environment.TickCount;
            labelTime.Text = (end - start).ToString();
            //labelCount.Text = skyLine1.GetElevationPointCount().ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            skyLine1.Left();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            skyLine1.Right();
        }
    }
}
