using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Android.Util;
using Peaks360App.Providers;
using Peaks360App.Utilities;
using Peaks360App.Views.Camera;
using Peaks360Lib.Utilities;
using Java.Util;
using Peaks360Lib.Domain.Models;
using Xamarin.Essentials;

namespace Peaks360App.AppContext
{
    public class AppContextLiveData : AppContextBase
    {
        private static AppContextLiveData _instance;

        private GpsLocationProvider _locationProvider { get; set; }
        private CompassProvider _compassProvider { get; set; }

        private System.Timers.Timer _locationTimer;

        private static object synchLock = new object();

        public override double Heading { get { return _compassProvider.Heading; } }

        private float GetViewAngleHorizontal()
        {
            if (Settings.CameraPictureSize.Width == 0 || Settings.CameraPictureSize.Height == 0)
            {
                return Settings.AViewAngleHorizontal;
            }

            var dx = Settings.CameraResolutionSelected.Width / (float)Settings.CameraPictureSize.Width;
            var dy = Settings.CameraResolutionSelected.Height / (float)Settings.CameraPictureSize.Height;

            var m = (dx < dy) ? (dx / dy) : 1;
            return m * Settings.AViewAngleHorizontal;
        }

        private float GetViewAngleVertical()
        {
            if (Settings.CameraPictureSize.Width == 0 || Settings.CameraPictureSize.Height == 0)
            {
                return Settings.AViewAngleVertical;
            }

            var dx = Settings.CameraResolutionSelected.Width / (float)Settings.CameraPictureSize.Width;
            var dy = Settings.CameraResolutionSelected.Height / (float)Settings.CameraPictureSize.Height;

            var m = (dx > dy) ? (dy / dx) : 1;
            return m * Settings.AViewAngleVertical;
        }

        public override float ViewAngleHorizontal
        {
            get
            {
                var x = IsPortrait ? GetViewAngleVertical() : GetViewAngleHorizontal();
                return x;
            }
        }

        public override float ViewAngleVertical
        {
            get
            {;
                var x = IsPortrait ? GetViewAngleHorizontal() : GetViewAngleVertical();
                return x; 
            }
        }

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

            _locationTimer = new System.Timers.Timer();

            _locationTimer.Interval = 3000;
            _locationTimer.Elapsed += OnLocationTimerElapsed;

            _compassProvider.OnHeadingChanged = OnHeadingChanged;
        }

        public override void Initialize(Android.Content.Context context)
        {
            base.Initialize(context);

            Settings.LoadData(context);

            if (String.IsNullOrEmpty(Settings.CameraId))
            {
                Settings.CameraId = CameraUtilities.GetDefaultCamera();
                Settings.CameraResolutionSelected = CameraUtilities.GetDefaultCameraResolution(Settings.CameraId); 
            }

            var (viewAngleHorizontal, viewAngleVertical) = CameraUtilities.FetchCameraViewAngle(Settings.CameraId);
            var (resolutionHorizontal, resolutionVertical) = CameraUtilities.FetchCameraResolution(Settings.CameraId);

            Settings.SetCameraParameters(viewAngleHorizontal, viewAngleVertical, resolutionHorizontal, resolutionVertical);
        }

        public void OnHeadingChanged(double heading)
        {
            NotifyHeadingChanged(heading, HeadingCorrector);
        }

        public override void Start()
        {
            if (!CompassPaused)
            {
                _compassProvider.Start();
                _locationProvider.Start();
            }

            _locationTimer.Enabled = true;
        }

        private async void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (CompassPaused)
            {
                return;
            }

            bool needRefresh = await UpdateMyLocation();

            if (needRefresh)
            {
                ReloadData();
            }
        }

        protected override void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            base.OnSettingsChanged(sender, e);

            switch (e.ChangedData)
            {
                case ChangedData.GpsLocation:
                    Task.Run(async () =>
                    {
                        await UpdateMyLocation();
                        ReloadData();
                    });
                    break;
            }

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
                if (!Utilities.GpsUtils.HasAltitude(myLocation) 
                    || (Utilities.GpsUtils.HasAltitude(newLocation) && Math.Abs(newLocation.Altitude-myLocation.Altitude)>100))
                {
                    myLocation.Altitude = newLocation.Altitude;
                    needRefresh = true;
                }

                if (needRefresh)
                {
                    var poi = Database.GetNearestPoi(myLocation, iGpsUtilities);
                    if (poi != null)
                    {
                        myLocationPlaceInfo = new PlaceInfo( poi.Poi.Name, poi.Poi.Country);
                    }
                    else
                    {
                        myLocationPlaceInfo = (await PlaceNameProvider.AsyncGetPlaceName(myLocation));
                    }
                }

                return needRefresh;
            }
            catch (Exception ex)
            {
                LogError("Location update error", ex);
                return false;
            }
        }

        protected override void StartProviders()
        {
            _compassProvider.Start();
            _locationTimer.Start();
        }

        protected override void StopProviders()
        {
            _compassProvider.Stop();
            _locationTimer.Stop();
        }
    }
}