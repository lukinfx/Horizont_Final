using System;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;

namespace Peaks360Lib.Utilities
{
    public class ElevationDataGenerator
    {
        public const int MIN_PROFILE_DISTANCE = 500;//500 meters
        public const int MIN_PROFILE_DISTANCE_STEP = 100;//100 meters

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

            for (double d = MIN_PROFILE_DISTANCE; d < maxDistance; d += Math.Min(MIN_PROFILE_DISTANCE_STEP, d/100))
            {
                var x = GpsUtils.QuickGetGeoLocation(myLocation, d, angle);
                //int size = d < 5000 ? 1 : 3;
                int size = Math.Min(((int)d / 20000) + 1, 4);//0-20:1 20-40:2 40-60:3 60-100:4
                if (etc.TryGetElevation(x, out var elevation, size))
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
