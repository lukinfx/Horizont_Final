using Xamarin.Forms;
using Peaks360App.Services;

[assembly: Dependency(typeof(GpsService))]
namespace Peaks360App.Services
{
    public interface IGpsService
    {
        bool IsGpsAvailable();
    }

    public class GpsService : IGpsService
    {
        public bool IsGpsAvailable()
        {
            Android.Locations.LocationManager manager = (Android.Locations.LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);
            return manager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider);
        }
   }
}