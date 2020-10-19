using System;
using System.Threading.Tasks;
using System.Timers;
using HorizontApp.Providers;
using HorizontApp.Utilities;
using HorizontLib.Utilities;
using Xamarin.Essentials;

namespace HorizontApp.AppContext
{
    public class AppContextLiveData : AppContextBase
    {
        private static AppContextLiveData _instance;

        private GpsLocationProvider GpsLocationProvider { get; set; }
        private CompassProvider CompassProvider { get; set; }

        //TODO:Move _headingStabilizator to _compassProvider class
        private HeadingStabilizator HeadingStabilizator { get; set; }

        private Timer _compassTimer;
        private Timer _locationTimer;

        public static IAppContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppContextLiveData();
                }

                return _instance;
            }
        }

        private AppContextLiveData()
        {
            GpsLocationProvider = new GpsLocationProvider();
            CompassProvider = new CompassProvider();
            HeadingStabilizator = new HeadingStabilizator();

            _compassTimer = new Timer();
            _locationTimer = new Timer();

            _compassTimer.Interval = 100;
            _compassTimer.Elapsed += OnCompassTimerElapsed;
            _compassTimer.Enabled = true;

            _locationTimer.Interval = 3000;
            _locationTimer.Elapsed += OnLocationTimerElapsed;
            _locationTimer.Enabled = true;

             CompassProvider.Start();
             GpsLocationProvider.Start();
        }

        private async void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            bool needRefresh = await UpdateMyLocation();

            if (needRefresh)
            {
                ReloadData();
            }
        }

        private void OnCompassTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!CompassPaused)
                {
                    HeadingStabilizator.AddValue(CompassProvider.Heading);
                    Heading = HeadingStabilizator.GetHeading();
                }
            });
        }

        private async Task<bool> UpdateMyLocation()
        {
            var newLocation = await GpsLocationProvider.GetLocationAsync();

            if (newLocation == null)
                return false;

            var distance = Utilities.GpsUtils.Distance(myLocation, newLocation);
            if (distance < 100 && Math.Abs(myLocation.Altitude - newLocation.Altitude) < 50)
                return false;

            bool needRefresh = false;
            if (distance > 100)
            {
                myLocation.Latitude = newLocation.Latitude;
                myLocation.Longitude = newLocation.Longitude;
                needRefresh = true;
            }

            //keep old location if new location has no altitude
            if (!Utilities.GpsUtils.HasAltitude(myLocation) || Utilities.GpsUtils.HasAltitude(newLocation))
            {
                myLocation.Altitude = newLocation.Altitude;
                needRefresh = true;
            }

            return needRefresh;
        }
    }
}