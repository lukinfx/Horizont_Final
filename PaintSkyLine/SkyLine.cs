﻿using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ElevationData;
using PaintSkyLine;

namespace PaintSkyLine
{
    public class SkyLine : Control
    {
        private static readonly int DG_WIDTH = 20;
        private static readonly int MIN_DISTANCE = 2500;

        private List<GeoPoint> _data;
        private double[] _elevationProfile = new double[360];
        private List<GeoPoint> _data2 = new List<GeoPoint>();
        private int _heading = 0;
        private double _visibility = 10;
        private GeoPoint _myLocation = new GeoPoint(49.4894558, 18.4914856, 830);

        public SkyLine()
        {
            //od radhoste po ropici
            /*double filterLatMin = 49.5250019;
            double filterLonMin = 18.3123825;
            double filterLatMax = 49.6626678;
            double filterLonMax = 18.5808428;*/

            //odnrejnik smrk a lysa
            double filterLatMin = 49.5248986;
            double filterLonMin = 18.2617339;
            double filterLatMax = 49.5946578;
            double filterLonMax = 18.5352992;

        }

        public void SetMyLocation(GeoPoint myLocation)
        {
            _myLocation = myLocation;
        }

        public void SetVisibility(double visibility)
        {
            _visibility = visibility;
        }

        public void Draw()
        {
            _myLocation.BoundingRect(_visibility, out var min, out var max);
            
            string inputFileName = @"c:\Temp\ElevationMap\ALPSMLC30_N049E018_DSM.tif";

            /*{
                var myData = ElevationData.GeoTiffReader.ReadTiff2(inputFileName, min.Latitude, max.Latitude, min.Longitude, max.Longitude);
                var y = 3599-(_myLocation.Latitude - (int) _myLocation.Latitude) / (1 / 3600.0);
                var x = (_myLocation.Longitude - (int)_myLocation.Longitude) / (1 / 3600.0);
                var ele = myData[(int)y, (int)x];
            }*/


            _data = ElevationData.GeoTiffReader.QuickReadTiff(inputFileName, _myLocation, min.Latitude, max.Latitude, min.Longitude, max.Longitude);

            for (int i=0; i<360; i++)
            {
                _elevationProfile[i] = -90;
            }

            foreach (var point in _data)
            {

                var dist = point.Distance;
                var bearing = GeoPoint.Normalize360(point.Bearing);
                var verticalAngle = point.VerticalAngle;

                if (dist > MIN_DISTANCE && dist < _visibility * 1000)
                {
                    int dg = (int) Math.Floor(bearing);
                    if (verticalAngle > _elevationProfile[dg])
                    {
                        _elevationProfile[dg] = verticalAngle;
                    }
                }
            }

            Invalidate();
        }

        public void Right()
        {
            _heading += 10;
            _heading = _heading % 360;
            Invalidate();
        }

        public void Left()
        {
            _heading -= 10;
            if (_heading < 0)
            {
                _heading += 360;
            }
            _heading = _heading % 360;
            Invalidate();
        }

        void PaintTerrain(PaintEventArgs e)
        {
            var pen = new Pen(Brushes.Black, 10);
            _data2 = new List<GeoPoint>();
            var sortedData = _data
                .Where(i =>
                    i.Distance > MIN_DISTANCE && i.Distance < _visibility * 1000
                    && GeoPoint.IsAngleBetween(i.Bearing, _heading, 35))
                .OrderBy(i => i.Distance);

            var z = _data
                .Where(i => i.Distance > MIN_DISTANCE && i.Distance < _visibility * 1000)
                .GroupBy(i => Math.Floor(i.Bearing));

            foreach (var i in z)
            {
                var bearing = i.Key;
                var points = i.OrderBy(i2 => i2.Distance);
                List<GeoPoint> displayedPoints = new List<GeoPoint>();
                foreach (var point in points)
                {

                    bool display = true;
                    foreach (var poi in displayedPoints)
                    {
                        if (point.VerticalAngle < poi.VerticalAngle)
                        {
                            display = false;
                            break;
                        }
                    }
                    if (display || displayedPoints.Count == 0)
                    {
                        displayedPoints.Add(point);
                    }
                }

                displayedPoints.OrderByDescending(j => j.Distance);
                
                foreach (var point in displayedPoints)
                {
                    bool display = true;
                    foreach (var otherPoint in displayedPoints)
                    {
                        if (point.Altitude < otherPoint.Altitude && Math.Abs(point.Distance - otherPoint.Distance) < 500)
                        {
                            display = false;
                        }
                    }

                    if (display)
                    {

                        var b = GeoPoint.Normalize360(point.Bearing - _heading);
                        var c = (point.Distance / (_visibility * 1000)) * 200;
                        pen.Color = Color.FromArgb(100, (int)c, (int)c, (int)c);
                        var x = GeoPoint.Normalize360(b + 35) * DG_WIDTH;
                        var y = 250 - point.VerticalAngle * 40;
                        e.Graphics.DrawLine(pen, (float)x, (float)y - 10, (float)x, (float)y);
                        _data2.Add(point);
                    }
                }

            }

            //foreach (var point in sortedData)
            //{
            //    var b = GeoPoint.Normalize360(point.Bearing - _heading);
            //    var c = (point.Distance / (_visibility * 1000)) * 200;
            //    pen.Color = Color.FromArgb(100, (int)c, (int)c, (int)c);
            //    var x = GeoPoint.Normalize360(b + 35) * DG_WIDTH;
            //    var y = 250 - point.VerticalAngle * 40;
            //    if (IsMax(point))
            //    {
            //        e.Graphics.DrawLine(pen, (float)x, (float)y-10, (float)x, (float)y);
            //    }
                
            //}
        }

        void PaintProfile2(PaintEventArgs e)
        {
            _data2.OrderBy(i => i.Distance);

            var pen = new Pen(Brushes.Black, 3);

            foreach (var point in _data2)
            {
                foreach (var otherPoint in _data2)
                {
                    if (Math.Abs(point.Distance - otherPoint.Distance)< point.Distance / 10 && Math.Abs(point.Bearing - otherPoint.Bearing) < 2)
                    {
                        var b1 = GeoPoint.Normalize360(point.Bearing - _heading);
                        var x1 = GeoPoint.Normalize360(b1 + 35) * DG_WIDTH;
                        var y1 = 250 - point.VerticalAngle * 40;

                        var b2 = GeoPoint.Normalize360(otherPoint.Bearing - _heading);
                        var x2 = GeoPoint.Normalize360(b2 + 35) * DG_WIDTH;
                        var y2 = 250 - otherPoint.VerticalAngle * 40;
                        if (Math.Sqrt(Math.Pow(x1-x2, 2)+ Math.Pow(y1 - y2, 2))<100)
                            e.Graphics.DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);
                        
                    }
                }
            }
        }

        void PaintProfile(PaintEventArgs e)
        {
            var points = new List<PointF>();

            for (int i = _heading - 35; i < _heading + 35; i++)
            {
                var dg = (i + 360) % 360;

                double y = _elevationProfile[dg] * 40;
                double x = (i - _heading + 35) * DG_WIDTH;

                points.Add(new PointF((float)x, (float)(250 - y)));

                //e.Graphics.DrawLine(new Pen(Brushes.Blue), (float)x, 250, (float)x, (float)(250-y));
                if (i % 10 == 0)
                {
                    e.Graphics.DrawString(i.ToString(), new Font("Arial", 10), new SolidBrush(Color.Black), (float)x, 10);
                }
            }

            
            points.Add(new PointF((float)69 * DG_WIDTH, this.Height));
            points.Add(new PointF((float)0 * DG_WIDTH, this.Height));
            e.Graphics.DrawPolygon(new Pen(Color.LightSkyBlue, 3), points.ToArray());
            //e.Graphics.FillPolygon(Brushes.LightSkyBlue, points.ToArray());

            e.Graphics.DrawLine(new Pen(Brushes.Black, 3), 0, 250, 69 * DG_WIDTH, 250);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(Brushes.White, 0, 0, this.Width, this.Height);

            if (_data == null)
                return;

            PaintTerrain(e);
            PaintProfile2(e);

            PaintProfile(e);
        }

        public int GetElevationPointCount()
        {
            return _data.Count();
        }

    }
}
