using System;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizonLib.Utilities
{
    public class ElevationDataGenerator
    {
        private ElevationProfileData _elevationProfileData;

        public ElevationDataGenerator()
        {
        }

        public ElevationProfileData GetProfile()
        {
            return _elevationProfileData;
        }

        public void Generate(GpsLocation _myLocation, double maxDistanceKm, ElevationTileCollection etc, Action<int> onProgressChange)
        {
            _elevationProfileData = new ElevationProfileData(_myLocation, maxDistanceKm);

            /*for (ushort angle = 0; angle < 360; angle++)
            {
                onProgressChange(angle);

                var ed = new ElevationData(angle);

                for (int d = 500; d < 12000; d += 25)
                {
                    var x = GpsUtils.QuickGetGeoLocation(_myLocation, d, angle);
                    if (etc.TryGetElevation(x, out var elevation, 1))
                    {
                        x.Altitude = elevation;
                        x.Distance = d;
                        x.Bearing = angle;
                        x.GetVerticalViewAngle(_myLocation);

                        ed.Add(x);
                    }
                }

                _elevationProfileData.Add(ed);
            }*/


            for (ushort a = 0; a < 360; a++)
            {
                onProgressChange(a);


                var ed = GetElevationDataForAngle(a, _myLocation, maxDistanceKm*1000, etc);

                _elevationProfileData.Add(ed);
            }
        }

        private ElevationData GetElevationDataForAngle(ushort angle, GpsLocation myLocation, double maxDistance, ElevationTileCollection etc)
        {
            var ed = new ElevationData(angle);
            for (int d = 500; d < Math.Min(maxDistance, 3000); d += 50)
            {
                var x = GpsUtils.QuickGetGeoLocation(myLocation, d, angle);
                if (etc.TryGetElevation(x, out var elevation, 1))
                {
                    x.Altitude = elevation;
                    x.Distance = d;
                    x.Bearing = angle;
                    x.GetVerticalViewAngle(myLocation);

                    ed.Add(x);
                }
            }
            for (int d = 3000; d < Math.Min(maxDistance, 8000); d += 100)
            {
                var x = GpsUtils.QuickGetGeoLocation(myLocation, d, angle);
                if (etc.TryGetElevation(x, out var elevation, 2))
                {
                    x.Altitude = elevation;
                    x.Distance = d;
                    x.Bearing = angle;
                    x.GetVerticalViewAngle(myLocation);

                    ed.Add(x);
                }
            }

            for (int d = 8000; d < Math.Min(maxDistance, 15000); d += 200)
            {
                var x = GpsUtils.QuickGetGeoLocation(myLocation, d, angle);
                if (etc.TryGetElevation(x, out var elevation, 3))
                {
                    x.Altitude = elevation;
                    x.Distance = d;
                    x.Bearing = angle;
                    x.GetVerticalViewAngle(myLocation);

                    ed.Add(x);
                }
            }
            for (int d = 15000; d < Math.Min(maxDistance, 50000); d += 400)
            {
                var x = GpsUtils.QuickGetGeoLocation(myLocation, d, angle);
                if (etc.TryGetElevation(x, out var elevation, 5))
                {
                    x.Altitude = elevation;
                    x.Distance = d;
                    x.Bearing = angle;
                    x.GetVerticalViewAngle(myLocation);

                    ed.Add(x);
                }
            }
            for (int d = 50000; d < Math.Min(maxDistance, 100000); d += 800)
            {
                var x = GpsUtils.QuickGetGeoLocation(myLocation, d, angle);
                if (etc.TryGetElevation(x, out var elevation, 10))
                {
                    x.Altitude = elevation;
                    x.Distance = d;
                    x.Bearing = angle;
                    x.GetVerticalViewAngle(myLocation);

                    ed.Add(x);
                }
            }

            return ed;
        }
    }
}
