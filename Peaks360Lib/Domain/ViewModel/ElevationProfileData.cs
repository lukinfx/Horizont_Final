using System;
using System.Linq;
using System.Collections.Generic;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Extensions;
using Peaks360Lib.Utilities;
using Newtonsoft.Json;

namespace Peaks360Lib.Domain.ViewModel
{
    public class ElevationProfileData
    {
        //distance/altitude limit where elevation profile is NOT recalculated (in meters)
        public const int VALIDITY_DISTANCE_MAX1 = 100; 
        public const int VALIDITY_ALTITUDE_MAX1 = 100;

        //distance/altitude limit where elevation profile is NOT hidden (in meters)
        public const int VALIDITY_DISTANCE_MAX2 = 200;
        public const int VALIDITY_ALTITUDE_MAX2 = 200;

        public GpsLocation MyLocation { get; set; }
        public double MaxDistance { get; set; }

        public List<ElevationData> elevationData = new List<ElevationData>();
        public string ErrorMessage { get; set; }

        public ElevationProfileData(GpsLocation myLocation, double maxDistance)
        {
            MyLocation = new GpsLocation(myLocation.Longitude, myLocation.Latitude, myLocation.Altitude);
            MaxDistance = maxDistance;
        }

        public ElevationProfileData(string errMsg = null)
        {
            ErrorMessage = errMsg;
        }

        private ElevationProfileData()
        {
            //this constructor is only for Deserialize method
        }

        public void Add(ElevationData ed)
        {
            elevationData.Add(ed);
        }

        public static ElevationProfileData Deserialize(string serializedData)
        {
            if (!string.IsNullOrEmpty(serializedData))
            {
                return JsonConvert.DeserializeObject<ElevationProfileData>(serializedData);
            }
            else
            {
                return new ElevationProfileData(new GpsLocation(0, 0, 0), 0);
            }
        }

        public string Serialize()
        {
            if (MaxDistance.IsEqual(0, 0.1))
                return null;

            var epd = new ElevationProfileData();
            return JsonConvert.SerializeObject(this);
        }


        public List<ElevationData> GetData()
        {
            return elevationData;
        }

        public ElevationData GetData(int angle)
        {
            return elevationData.SingleOrDefault(i => i.Angle == GpsUtils.Normalize360(angle));
        }

        public bool IsLocationNearBy(GpsLocation newLocation)
        {
            if (GpsUtils.QuickDistance(newLocation, MyLocation) > VALIDITY_DISTANCE_MAX2)
                return false;

            if (Math.Abs(newLocation.Altitude - MyLocation.Altitude) > VALIDITY_ALTITUDE_MAX2)
                return false; 
            
            return true;
        }

        public bool IsValid(GpsLocation newLocation, double newMaxDistance)
        {
            //If me move more than 100m
            if (GpsUtils.QuickDistance(newLocation, MyLocation) > VALIDITY_DISTANCE_MAX1)
                return false;
            
            if (Math.Abs(newLocation.Altitude - MyLocation.Altitude) > VALIDITY_ALTITUDE_MAX1)
                return false;

            if (newMaxDistance > MaxDistance)
                return false;

            return true;
        }

        /*public void ResetPointConnection()
        {
            for (ushort i = 0; i < 360; i++)
            {
                var thisAngle = GetData(i);
                foreach (var p in thisAngle.GetPoints())
                {
                    p.AlreadyConnected = false;
                }
            }
        }*/

        public List<ProfileLine> GetProfileLines()
        {
            //ResetPointConnection();

            List<ProfileLine> listOfLines = new List<ProfileLine>();
            for (ushort i = 0; i < 360; i++)
            {
                var thisAngle = GetData(i);
                var prevAngle = GetData(i - 1);

                if (thisAngle != null && prevAngle != null)
                {
                    foreach (var point in thisAngle.GetPoints())
                    {
                        var otherPoint = prevAngle.GetPoints()
                            .Where(x => 
                                Math.Abs(point.Distance.Value - x.Distance.Value) <= point.Distance.Value / 12 
                                /*&& !x.AlreadyConnected*/
                            )
                            .OrderBy(x => Math.Abs(point.Distance.Value - x.Distance.Value))
                            .FirstOrDefault();

                        if (otherPoint != null)
                        {
                            var y1 = point.VerticalViewAngle.Value;
                            var x1 = (float)point.Bearing.Value;

                            var y2 = otherPoint.VerticalViewAngle.Value;
                            var x2 = (float)otherPoint.Bearing.Value;

                            listOfLines.Add(new ProfileLine { Bearing1 = x1, Bearing2 = x2, VerticalViewAngle1 = (float)y1, VerticalViewAngle2 = (float)y2, distance = point.Distance.Value });

                            //otherPoint.AlreadyConnected = true;
                        }
                    }
                }
            }
            return listOfLines;
        }

    }
}