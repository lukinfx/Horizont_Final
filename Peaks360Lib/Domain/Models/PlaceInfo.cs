using Peaks360Lib.Domain.Enums;

namespace Peaks360Lib.Domain.Models
{
    public class PlaceInfo
    {
        public PlaceInfo(string placeName = "?????", PoiCountry? country = null)
        {
            PlaceName = placeName;
            Country = country;
        }
        public string PlaceName { get; private set; }
        public PoiCountry? Country { get; private set; }
    }
}
