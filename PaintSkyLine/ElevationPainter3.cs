using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HorizonLib.Utilities;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;

namespace PaintSkyLine
{
    class ElevationPainter3
    {
        public List<GpsLocation> list = new List<GpsLocation>();

        public ElevationPainter3()
        {
        }

        public void Generate(GpsLocation _myLocation, ElevationTileCollection etc)
        {
            list.Clear();

            for (int a = 0; a < 360; a++)
            {
                for (int d = 500; d < 3000; d += 50)
                {
                    var x = GpsUtils.QuickGetGeoLocation(_myLocation, d, a);
                    if (etc.TryGetElevation(x, out var elevation, 1))
                    {
                        x.Altitude = elevation; 
                        x.Distance = d;
                        x.Bearing = a;
                        x.GetVerticalViewAngle(_myLocation);

                        list.Add(x);
                    }
                }
                for (int d = 3000; d < 8000; d += 100)
                {
                    var x = GpsUtils.QuickGetGeoLocation(_myLocation, d, a);
                    if (etc.TryGetElevation(x, out var elevation, 2))
                    {
                        x.Altitude = elevation;
                        x.Distance = d;
                        x.Bearing = a;
                        x.GetVerticalViewAngle(_myLocation);

                        list.Add(x);
                    }
                }

                for (int d = 8000; d < 15000; d += 200)
                {
                    var x = GpsUtils.QuickGetGeoLocation(_myLocation, d, a);
                    if (etc.TryGetElevation(x, out var elevation, 3))
                    {
                        x.Altitude = elevation;
                        x.Distance = d;
                        x.Bearing = a;
                        x.GetVerticalViewAngle(_myLocation);

                        list.Add(x);
                    }
                }
                for (int d = 15000; d < 50000; d += 400)
                {
                    var x = GpsUtils.QuickGetGeoLocation(_myLocation, d, a);
                    if (etc.TryGetElevation(x, out var elevation, 5))
                    {
                        x.Altitude = elevation;
                        x.Distance = d;
                        x.Bearing = a;
                        x.GetVerticalViewAngle(_myLocation);

                        list.Add(x);
                    }
                }
                for (int d = 50000; d < 100000; d += 800)
                {
                    var x = GpsUtils.QuickGetGeoLocation(_myLocation, d, a);
                    if (etc.TryGetElevation(x, out var elevation, 10))
                    {
                        x.Altitude = elevation;
                        x.Distance = d;
                        x.Bearing = a;
                        x.GetVerticalViewAngle(_myLocation);

                        list.Add(x);
                    }
                }
            }

            /*int c = 0;
            foreach (var et in etc)
            {
                c++;
                if (et.QuickDistance(_myLocation) > 50000)
                    continue;
            }*/
        }

    }
}
