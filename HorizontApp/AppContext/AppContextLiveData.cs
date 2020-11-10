﻿using System;
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

        private static object synchLock = new object();

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
            GpsLocationProvider = new GpsLocationProvider();
            CompassProvider = new CompassProvider();
            HeadingStabilizator = new HeadingStabilizator();

            _compassTimer = new Timer();
            _locationTimer = new Timer();

            _compassTimer.Interval = 100;
            _compassTimer.Elapsed += OnCompassTimerElapsed;

            _locationTimer.Interval = 3000;
            _locationTimer.Elapsed += OnLocationTimerElapsed;
        }

        public override void Start()
        {
            CompassProvider.Start();
            GpsLocationProvider.Start();

            _compassTimer.Enabled = true;
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

        private void OnCompassTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!CompassPaused)
                {
                    HeadingStabilizator.AddValue(CompassProvider.Heading);
                    Heading = HeadingStabilizator.GetHeading();
                    if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
                    {
                        Heading = Heading - 90;
                    }
                }
            });
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
                var newLocation = await GpsLocationProvider.GetLocationAsync();

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

            _compassTimer.Stop();
            _locationTimer.Stop();
        }

        public override void Resume()
        {
            base.Resume();

            _compassTimer.Start();
            _locationTimer.Start();
        }

    }
}