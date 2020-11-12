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

        private GpsLocationProvider _locationProvider { get; set; }
        private CompassProvider _compassProvider { get; set; }

        //TODO:Move _headingStabilizator to _compassProvider class
        private HeadingStabilizator HeadingStabilizator { get; set; }

        private Timer _compassTimer;
        private Timer _locationTimer;

        private static object synchLock = new object();

        public override double Heading { get { return _compassProvider.Heading; } }

        public static IAppContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (synchLock)
                    {
                        if (_instance == null)
                            _instance = new AppContextLiveData();
                    }
                }

                return _instance;
            }
        }

        private AppContextLiveData()
        {
            _locationProvider = new GpsLocationProvider();
            _compassProvider = new CompassProvider();

            _locationTimer = new Timer();

            _locationTimer.Interval = 3000;
            _locationTimer.Elapsed += OnLocationTimerElapsed;
        }

        public override void Start()
        {
            _compassProvider.Start();
            _locationProvider.Start();

            _locationTimer.Enabled = true;
        }

        private async void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            bool needRefresh = await UpdateMyLocation();

            if (needRefresh)
            {
                ReloadData();
            }
        }

        protected override void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            base.OnSettingsChanged(sender, e);

            Settings.SaveData();
        }

        private async Task<bool> UpdateMyLocation()
        {
            try
            {
                var newLocation = await _locationProvider.GetLocationAsync();

                if (newLocation == null)
                    return false;

                var distance = Utilities.GpsUtils.Distance(myLocation, newLocation);
                if (distance < 100 && Math.Abs(myLocation.Altitude - newLocation.Altitude) < 30)
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
            catch (Exception ex)
            {
                LogError("Location update error", ex);
                return false;
            }
        }

        public override void Pause()
        {
            base.Pause();

            _compassProvider.Stop();
            _locationTimer.Stop();
        }

        public override void Resume()
        {
            base.Resume();

            _compassProvider.Start();
            _locationTimer.Start();
        }

    }
}