using System.Globalization;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class PoiCountryHelper
    {
        public static PoiCountry? GetDefaultCountry()
        {
            var regionCode = RegionInfo.CurrentRegion;
            if (PoiCountry.TryParse<PoiCountry>(regionCode.ThreeLetterISORegionName, out var poiCountry))
            {
                return poiCountry;
            }

            return null;
        }
    }
}