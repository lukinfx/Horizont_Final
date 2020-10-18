using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.DataAccess;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Providers;
using HorizontApp.Utilities;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;
using Xamarin.Essentials;

namespace HorizontApp.Utilities
{
    public class AppContext
    {
        public class DataChangedEventArgs : EventArgs { public PoiViewItemList PoiData; }
        public delegate void DataChangedEventHandler(object sender, DataChangedEventArgs e);
        public event DataChangedEventHandler DataChanged;

        private static AppContext _instance;
        
        private GpsLocationProvider GpsLocationProvider { get; set; }
        private CompassProvider CompassProvider { get; set; }
        public Settings Settings { get; private set; }
        public ElevationProfileData ElevationProfileData { get; set; }

        public bool CompassPaused { get; set; }

        //TODO:Move _headingStabilizator to _compassProvider class
        private HeadingStabilizator HeadingStabilizator { get; set; }

        private Timer _compassTimer;
        private Timer _locationTimer;
        
        private GpsLocation _myLocation = new GpsLocation();
        public GpsLocation MyLocation { get { return _myLocation; } }
        public double Heading { get; private set; }

        public PoiViewItemList PoiData { get; private set; }

        private PoiDatabase _database;
        public PoiDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new PoiDatabase();
                }
                return _database;
            }
        }

        public static AppContext Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new AppContext();
                }

                return _instance;
            }
        }

        public void ToggleCompassPaused()
        {
            CompassPaused = !CompassPaused;
        }

        private AppContext()
        {
            PoiData = new PoiViewItemList();
            GpsLocationProvider = new GpsLocationProvider();
            CompassProvider = new CompassProvider();
            HeadingStabilizator = new HeadingStabilizator();
            Settings = new Settings();

            _compassTimer = new Timer();
            _locationTimer = new Timer();
            
            _compassTimer.Interval = 100;
            _compassTimer.Elapsed += OnCompassTimerElapsed;
            _compassTimer.Enabled = true;

            _locationTimer.Interval = 3000;
            _locationTimer.Elapsed += OnLocationTimerElapsed;
            _locationTimer.Enabled = true;

            Settings.SettingsChanged += OnSettingsChanged;

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

        private void ReloadData()
        {
            try
            {
                if (GpsUtils.HasLocation(MyLocation))
                {
                    var poiList = Database.GetItems(MyLocation, Settings.MaxDistance);
                    PoiData = new PoiViewItemList(poiList, MyLocation, Settings.MaxDistance, Settings.MinAltitute, Settings.Favourite);
                }

                var args = new DataChangedEventArgs() {PoiData = PoiData};
                DataChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogError("Error when fetching data.", ex);
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

        private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            Settings.SaveData();

            ReloadData();
        }

        private async Task<bool> UpdateMyLocation()
        {
            var newLocation = await GpsLocationProvider.GetLocationAsync();

            if (newLocation == null)
                return false;

            var distance = GpsUtils.Distance(_myLocation, newLocation);
            if (distance < 100 && Math.Abs(_myLocation.Altitude - newLocation.Altitude) < 50)
                return false;

            bool needRefresh = false;
            if (distance > 100)
            {
                _myLocation.Latitude = newLocation.Latitude;
                _myLocation.Longitude = newLocation.Longitude;
                needRefresh = true;
            }

            //keep old location if new location has no altitude
            if (!GpsUtils.HasAltitude(_myLocation) || GpsUtils.HasAltitude(newLocation))
            {
                _myLocation.Altitude = newLocation.Altitude;
                needRefresh = true;
            }

            return needRefresh;
        }

        private void LogError(string v, Exception ex)
        {
            //TODO: logging
        }
    }
}