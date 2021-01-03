using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Android.Util;
using HorizontApp.Providers;
using HorizontApp.Utilities;
using HorizontApp.Views.Camera;
using HorizontLib.Utilities;
using Java.Util;
using Xamarin.Essentials;

namespace HorizontApp.AppContext
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
            var dx = Settings.cameraResolutionSelected.Width / (float)Settings.CameraPictureSize.Width;
            var dy = Settings.cameraResolutionSelected.Height / (float)Settings.CameraPictureSize.Height;

            var m = (dx < dy) ? (dx / dy) : 1;
            return m * Settings.AViewAngleHorizontal;
        }

        private float GetViewAngleVertical()
        {
            var dx = Settings.cameraResolutionSelected.Width / (float)Settings.CameraPictureSize.Width;
            var dy = Settings.cameraResolutionSelected.Height / (float)Settings.CameraPictureSize.Height;

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
                var cameraId = CameraUtilities.GetCameras().First();
                var listOfSizes = CameraUtilities.GetCameraResolutions(cameraId);
                Size defaultSize = (Size)Collections.Max(listOfSizes, new CompareSizesByArea());

                Settings.cameraResolutionSelected = defaultSize;
                Settings.CameraId = cameraId;
            }

            var (viewAngleHorizontal, viewAngleVertical) = CameraUtilities.FetchCameraViewAngle(Settings.CameraId);
            var (resolutionHorizontal, resolutionVertical) = CameraUtilities.FetchCameraResolution(Settings.CameraId);

            Settings.SetCameraParameters(viewAngleHorizontal, viewAngleVertical, resolutionHorizontal, resolutionVertical);
        }

        public void OnHeadingChanged(double heading)
        {
            NotifyHeadingChanged(heading);
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
            NotifyDataChanged();
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