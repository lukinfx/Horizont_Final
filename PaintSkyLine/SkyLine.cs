using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.XPath;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using PaintSkyLine;
using SkiaSharp;

namespace PaintSkyLine
{
    public class SkyLine : Control
    {
        private static readonly int DG_WIDTH = 50;
        private static readonly int MIN_DISTANCE = 1000;

        private List<GpsLocation> _data;
        //private ElevationProfileData elevationProfileOld;
        private ElevationProfileData elevationProfileNew;
        private ElevationTileCollection elevationTileCollection;

        ElevationDataGenerator profileGenerator;

        private int _heading = 0;
        private int _visibilityKm = 10;
        private int _minDist = 10;
        private GpsLocation _myLocation = new GpsLocation(49.4894558, 18.4914856, 830);

        private SKData profileImageData;

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

            profileGenerator = new ElevationDataGenerator();
        }

        public void SetMyLocation(GpsLocation myLocation)
        {
            _myLocation = myLocation;
        }

        public void SetMinDist(int minDist)
        {
            _minDist = minDist;
        }

        public void SetVisibility(int visibility)
        {
            _visibilityKm = visibility;
        }

        public void CalculateProfile()
        {
            GpsUtils.BoundingRect(_myLocation,_visibilityKm*1000, out var min, out var max);

            /*_data = new List<GpsLocation>();
            string inputFileName = @"c:\Temp\ElevationMap\ALPSMLC30_N049E018_DSM.tif";
            GeoTiffReaderList.ReadTiff(inputFileName, min, max, _myLocation, 3, _data);*/

            //Calculate old profile
            /*ElevationProfile ep = new ElevationProfile();
            ep.GenerateElevationProfile(_myLocation, _visibility, _data, progress => { });
            elevationProfileOld = ep.GetProfile();*/

            //Calucate new profile

            elevationTileCollection = new ElevationTileCollection(_myLocation, (int)_visibilityKm);
            var d = elevationTileCollection.GetSizeToDownload();
            elevationTileCollection.Download(progress => { });
            elevationTileCollection.Read(progress => { });
            profileGenerator.Generate(_myLocation, 12, elevationTileCollection, progress => { });

            /*ProfileGeneratorOld ep2 = new ProfileGeneratorOld();
            //ep2.GenerateElevationProfile3(_myLocation, _visibility, _data, progress => { });
            ep2.GenerateElevationProfile3(_myLocation, _visibility, elevationPainter3.list, progress => { });
            elevationProfileNew = ep2.GetProfile();*/

            ElevationProfile ep3 = new ElevationProfile();
            ep3.GenerateElevationProfile3(_myLocation, _visibilityKm, profileGenerator.GetProfile(), progress => { });
            elevationProfileNew = ep3.GetProfile();

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
            var pen = new Pen(Brushes.Black, 20);
            var _data2 = new List<GpsLocation>();
            var sortedData = _data
                .Where(i =>
                    i.Distance > MIN_DISTANCE && i.Distance < _visibilityKm * 1000
                    && GpsUtils.IsAngleBetween(i.Bearing.Value, _heading, 35))
                .OrderByDescending(i2 => i2.Distance);
            foreach (var point in sortedData)
            {

                var b = GpsUtils.Normalize360(point.Bearing.Value - _heading);
                var c = (point.Distance / (_visibilityKm * 1000)) * 200;
                pen.Color = Color.FromArgb(100, (int)c, (int)c, (int)c);
                var x = GpsUtils.Normalize360(b + 35) * DG_WIDTH;
                var y = 250 - point.VerticalViewAngle * 40;
                e.Graphics.DrawLine(pen, (float)x, (float)500, (float)x, (float)y);
            }
            var z = _data
                .Where(i => i.Distance > MIN_DISTANCE && i.Distance < _visibilityKm * 1000)
                .GroupBy(i => Math.Floor(i.Bearing.Value));

            foreach (var i in z)
            {
                var bearing = i.Key;
                var points = i.OrderBy(i2 => i2.Distance);
                List<GpsLocation> displayedPoints = new List<GpsLocation>();
                foreach (var point in points)
                {

                    bool display = true;
                    foreach (var poi in displayedPoints)
                    {
                        if (point.VerticalViewAngle < poi.VerticalViewAngle)
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
                        if (point.Altitude < otherPoint.Altitude && Math.Abs(point.Distance.Value - otherPoint.Distance.Value) < 500)
                        {
                            display = false;
                        }
                    }

                    if (display)
                    {
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
            List<GpsLocation> lst;
            if (elevationProfileNew == null)
            {
                return;
            }

            var pen = new Pen(Brushes.Black, 3);

            var data = elevationProfileNew.GetData();
            for(ushort i = 0; i < 360; i++)
            {
                var thisAngle = elevationProfileNew.GetData(i);
                var prevAngle = elevationProfileNew.GetData(i - 1);

                if (thisAngle != null && prevAngle != null)
                {
                    foreach (var point in thisAngle.GetPoints())
                    {
                        foreach (var otherPoint in prevAngle.GetPoints())
                        {
                            if (Math.Abs(point.Distance.Value - otherPoint.Distance.Value) < point.Distance / _minDist)
                            {
                                var b1 = GpsUtils.Normalize360(point.Bearing.Value - _heading);
                                var x1 = GpsUtils.Normalize360(b1 + 35) * DG_WIDTH;
                                var y1 = 250 - point.VerticalViewAngle.Value * 40;

                                var b2 = GpsUtils.Normalize360(otherPoint.Bearing.Value - _heading);
                                var x2 = GpsUtils.Normalize360(b2 + 35) * DG_WIDTH;
                                var y2 = 250 - otherPoint.VerticalViewAngle.Value * 40;
                                if (Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)) < 100)
                                    e.Graphics.DrawLine(pen, (float) x1, (float) y1, (float) x2, (float) y2);
                            }
                        }
                    }
                }
            }
        }

        void PaintProfileScale(PaintEventArgs e)
        {
            for (int i = _heading - 35; i < _heading + 35; i++)
            {
                var dg = (i + 360) % 360;
                double x = (i - _heading + 35) * DG_WIDTH;

                //e.Graphics.DrawLine(new Pen(Brushes.Blue), (float)x, 250, (float)x, (float)(250-y));
                if (i % 10 == 0)
                {
                    e.Graphics.DrawString(i.ToString(), new Font("Arial", 10), new SolidBrush(Color.Black), (float)x, 10);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(Brushes.AntiqueWhite, 0, 0, this.Width, this.Height);

            PaintProfileScale(e);

            //PaintTerrain(e);
            //PaintProfile2(e);
            
            //PaintProfile2(e); last
            PaintTerrain2(e);
        }

        private void PaintTerrain2(PaintEventArgs e)
        {
            var aStep = 0.1f;//0.1dg
            double dStep = 50;//50m

            if (elevationTileCollection == null)
                return;

            double[,] elevationMap= new double[700, 10000];
            double[,] vva = new double[700, 10000];
            double[] maxvva = new double[700];


            var pen = new Pen(Brushes.Black, 1);
            //var pen2 = new Brush((Brushes.Blue, 1);
            
            int d2 = 0;

            for (int i = 0; i < 700; i++)
            {
                var angle = ((_heading + i/10f - 35) + 360) % 360;

                d2 = 0;
                maxvva[i] = 0;
                for (double d = 500; d < _visibilityKm*1000; d += dStep)
                {
                    var loc = GpsUtils.QuickGetGeoLocation(_myLocation, d, angle);
                    //int size = d < 5000 ? 1 : 3;
                    int size = 1; //Math.Min(((int)d / 20000) + 1, 4);//0-20:1 20-40:2 40-60:3 60-100:4
                    if (elevationTileCollection.TryGetElevation(loc, out var elevation, size))
                    {
                        loc.Altitude = elevation;
                        loc.Distance = d;
                        loc.Bearing = angle;
                        loc.GetVerticalViewAngle(_myLocation);
                        elevationMap[i, d2] = loc.Altitude;
                        var currentvva = loc.GetVerticalViewAngle(_myLocation);
                        vva[i, d2] = currentvva;
                        /*if (maxvva[i] < currentvva)
                        {
                            maxvva[i] = currentvva;
                            vva[i, d2] = currentvva;
                        }
                        else
                        {
                            vva[i, d2] = 0;
                        }*/
                    }

                    d2++;
                }
            }

            /*for (int a = 0; a < 70-1; a++)
            {
                double x1 = a * DG_WIDTH;
                double x2 = (a+1) * DG_WIDTH;

                
                for (int d = 0; d < d2-1; d++)
                {
                    var y1 = 250 - vva[a,d] * 40;
                    var y2 = 250 - vva[a+1, d] * 40;
                    
                    //var a1 = elevationMap[a, d];
                    //var a2 = elevationMap[a + 1, d];

                    if (y1 > 0 && y2 > 0)
                    {
                        e.Graphics.DrawLine(pen, (float) x1, (float) y1, (float) x2, (float) y2);
                    }
                }
            }*/

            float xLast = 0;
            for (int a = 1; a < 700-1; a++)
            {
                float xr = (a+aStep) * DG_WIDTH*aStep;
                float xl = xLast;
                xLast = xr;

                for (int d = d2-1; d > 0; d--)
                {
                    if (vva[a, d - 1] > 0 && vva[a + 1, d - 1] > 0 && vva[a, d] > 0)
                    {

                        var yl1 = (float) (250 - vva[a, d - 1] * 40);
                        var yr1 = (float) (250 - vva[a + 1, d - 1] * 40);
                        var yl2 = (float) (250 - vva[a, d] * 40);
                        var yr2 = (float)(250 - vva[a + 1, d] * 40);

                        var al1 = elevationMap[a, d - 1];
                        var ar1 = elevationMap[a + 1, d - 1];
                        var al2 = elevationMap[a, d];
                        var ar2 = elevationMap[a + 1, d];

                        var aDiff = ((al2 - al1) + (ar2 - ar1)) / 2;
                        var norm = Math.Atan(aDiff / dStep) / Math.PI * 180;
                        if (norm < 0)
                            continue;

                        if (norm > 180)
                            continue;

                        var col = norm / 90;
                        var col2 = 255-(int)Math.Min(col*2 * 255,255);
                        var b = new System.Drawing.SolidBrush(Color.FromArgb(255, col2, col2, col2));


                        //var a1 = elevationMap[a, d];
                        //var a2 = elevationMap[a + 1, d];

                        var p = new PointF[]
                        {
                            new PointF(xl, yl1),
                            new PointF(xl, yl2),
                            new PointF(xr, yr2),
                            new PointF(xr, yr1)
                        };

                        e.Graphics.FillPolygon(b, p);
                        //e.Graphics.DrawPolygon(pen, p);
                    }
                }
            }

            /*var sortedData = _data
                .Where(i =>
                    i.Distance > MIN_DISTANCE && i.Distance < _visibility * 1000
                    && GpsUtils.IsAngleBetween(i.Bearing.Value, _heading, 35))
                .OrderByDescending(i2 => i2.Distance);
            foreach (var point in sortedData)
            {

                var b = GpsUtils.Normalize360(point.Bearing.Value - _heading);
                var c = (point.Distance / (_visibility * 1000)) * 200;
                pen.Color = Color.FromArgb(100, (int)c, (int)c, (int)c);
                var x = GpsUtils.Normalize360(b + 35) * DG_WIDTH;
                var y = 250 - point.VerticalViewAngle * 40;
                e.Graphics.DrawLine(pen, (float)x, (float)500, (float)x, (float)y);
            }
            var z = _data
                .Where(i => i.Distance > MIN_DISTANCE && i.Distance < _visibility * 1000)
                .GroupBy(i => Math.Floor(i.Bearing.Value));

            foreach (var i in z)
            {
                var bearing = i.Key;
                var points = i.OrderBy(i2 => i2.Distance);
                List<GpsLocation> displayedPoints = new List<GpsLocation>();
                foreach (var point in points)
                {

                    bool display = true;
                    foreach (var poi in displayedPoints)
                    {
                        if (point.VerticalViewAngle < poi.VerticalViewAngle)
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
                        if (point.Altitude < otherPoint.Altitude && Math.Abs(point.Distance.Value - otherPoint.Distance.Value) < 500)
                        {
                            display = false;
                        }
                    }

                    if (display)
                    {
                        _data2.Add(point);
                    }
                }
            }*/
        }


        public int GetElevationPointCount()
        {
            return _data.Count();
        }

    }
}
