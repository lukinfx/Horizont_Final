using System.Linq;
using System.Collections.Generic;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;
using Newtonsoft.Json;

namespace HorizontLib.Domain.ViewModel
{
    public class ElevationProfileData
    {
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
            return JsonConvert.DeserializeObject<ElevationProfileData>(serializedData);
        }

        public string Serialize()
        {
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

        public bool IsValid(GpsLocation newLocation, double newMaxDistance)
        {
            //If me move more than 100m
            if (GpsUtils.QuickDistance(newLocation, MyLocation) > 0.1)
                return false;

            if (newMaxDistance > MaxDistance)
                return false;

            return true;
        }
    }
}