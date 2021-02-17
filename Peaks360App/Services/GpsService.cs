using Android.Content;
using Xamarin.Forms;
using Peaks360App.Services;

[assembly: Dependency(typeof(GpsService))]
namespace Peaks360App.Services
{
    public interface IGpsService
    {
        void OpenSettings();
        bool isGpsAvailable();
    }

    public class GpsService : IGpsService
    {
        public bool isGpsAvailable()
        {
            bool value = false;
            Android.Locations.LocationManager manager = (Android.Locations.LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);
            if (!manager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider))
            {
                //gps disable
                value = false;
            }
            else
            {
                //Gps enable
                value = true;
            }
            return value;
        }

        public void OpenSettings()
        {
            Intent intent = new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
    }
}