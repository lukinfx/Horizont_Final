using System.Linq;
using System.Collections.Generic;
using HorizontLib.Utilities;

namespace HorizontLib.Domain.ViewModel
{
    public class ElevationProfileData
    {
        public List<ElevationData> elevationData = new List<ElevationData>();
        public string ErrorMessage { get; set; }

        public ElevationProfileData(string errMsg)
        {
            ErrorMessage = errMsg;
        }

        public void Add(ElevationData ed)
        {
            elevationData.Add(ed);
        }

        public void Clear()
        {
            elevationData.Clear();
        }

        public List<ElevationData> GetData()
        {
            return elevationData;
        }

        public ElevationData GetData(int angle)
        {
            return elevationData.SingleOrDefault(i => i.Angle == GpsUtils.Normalize360(angle));
        }
    }
}