using System;
using System.Collections.Generic;
using System.Linq;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360Lib.Utilities
{
    public class ElevationProfile
    {
        private static readonly int MIN_DISTANCE = 300;
        private ElevationProfileData _elevationProfileData;

        public ElevationProfileData GetProfile()
        {
            return _elevationProfileData ?? throw new SystemException("Elevation profile was not generated yet");
        }

        public void GenerateElevationProfile3(GpsLocation myLocation, double visibility, ElevationProfileData elevationData, Action<int> onProgressChange)
        {
            _elevationProfileData = new ElevationProfileData(myLocation, visibility);

            int progress = 0;

            foreach (var group in elevationData.GetData())
            {
                progress++;
                onProgressChange(progress);

                var points = group.GetPoints()
                    .Where(i => i.Distance > MIN_DISTANCE && i.Distance < visibility * 1000)
                    .OrderBy(i => i.Distance);

                //Select visible points 
                List<GpsLocation> tmpVisiblePoints = new List<GpsLocation>();
                double maxViewAngle = -90;
                foreach (var point in points)
                {
                    if (point.VerticalViewAngle > maxViewAngle)
                    {
                        tmpVisiblePoints.Add(point);
                        maxViewAngle = point.VerticalViewAngle.Value;
                    }
                }

                //Change order (now from the farthest to the nearest) 
                tmpVisiblePoints.Reverse();

                //... and ignore points on descending slope 
                GpsLocation lastPoint = null;
                GpsLocation lastAddedPoint = null;
                var ed = new ElevationData(group.Angle);
                foreach (var point in tmpVisiblePoints)
                {
                    if (lastPoint == null)
                    {
                        ed.Add(point);
                        lastAddedPoint = point;
                        lastPoint = point;
                        continue;
                    }


                    if (lastPoint.Distance.Value - point.Distance.Value < 500)// || lastAddedPoint.VerticalViewAngle.Value-point.VerticalViewAngle.Value <= 0.00) 
                    {
                        lastPoint = point;
                        continue;
                    }

                    ed.Add(point);
                    lastAddedPoint = point;
                    lastPoint = point;
                }
                _elevationProfileData.Add(ed);
            }
        }
    }
}