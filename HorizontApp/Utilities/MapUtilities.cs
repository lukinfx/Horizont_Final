using HorizontLib.Domain.Models;
using Xamarin.Essentials;

namespace HorizontApp.Utilities
{
    public class MapUtilities
    {
        public static void OpenMap(double latitude, double longitude)
        {
            var location = new Location(latitude, longitude);
            Map.OpenAsync(location);
        }

        public static void OpenMap(Poi poi)
        {
            OpenMap(poi.Latitude, poi.Longitude);
        }
    }
}