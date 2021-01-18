using System;
using HorizontApp.DataAccess;
using HorizontLib.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontLib.Domain.Models;
using System.Collections.Generic;
using HorizonLib.Domain.Models;
using Xamarin.Essentials;
using HorizonLib.Domain.Enums;
using Android.Content.Res;
using Android.Content;

namespace HorizontApp.AppContext
{
    public abstract class AppContextBase : IAppContext
    {
        protected Context context;

        public virtual void Initialize(Context context)
        {
            this.context = context;
        }

        public event DataChangedEventHandler DataChanged;
        public event HeadingChangedEventHandler HeadingChanged;

        public Settings Settings { get; private set; }

        private ElevationProfileData _elevationProfileData;
        public ElevationProfileData ElevationProfileData
        {
            get
            {
                return _elevationProfileData;
            }
            set
            {
                _elevationProfileData = value;
                var args = new DataChangedEventArgs() { PoiData = PoiData };
                DataChanged?.Invoke(this, args);
            }
        }

        public bool CompassPaused { get; set; }
        
        protected GpsLocation myLocation = new GpsLocation();
        public GpsLocation MyLocation { get { return myLocation; } }
        public virtual double Heading { get; protected set; }
        public bool ShowFavoritesOnly { get; set; }
        public PoiViewItemList PoiData { get; protected set; }

        public bool IsPortrait
        {
            get {
                return  DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait &&
                    (DeviceDisplay.MainDisplayInfo.Rotation == DisplayRotation.Rotation0 || DeviceDisplay.MainDisplayInfo.Rotation == DisplayRotation.Rotation180);
            }
        }

        public abstract float ViewAngleHorizontal { get; }
        //virtual public float ViewAngleHorizontal { get; }
        public abstract float ViewAngleVertical { get; }

        protected PoiDatabase database;
        public PoiDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new PoiDatabase();
                }
                return database;
            }
        }

        public List<ProfileLine> ListOfProfileLines { get; set; }
        public double? ElevationProfileDataDistance { get; set; }

        public void ToggleCompassPaused()
        {
            CompassPaused = !CompassPaused;

            if (CompassPaused)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }

        public void ToggleFavourite()
        {
            ShowFavoritesOnly = !ShowFavoritesOnly;
        }

        protected AppContextBase()
        {
            PoiData = new PoiViewItemList();
            Settings = new Settings();
            Settings.SettingsChanged += OnSettingsChanged;
        }

        public abstract void Start();

        public void ReloadData()
        {
            try
            {
                NotifyDataChanged();
            }
            catch (Exception ex)
            {
                LogError("Error when fetching data.", ex);
            }
        }

        protected void NotifyDataChanged(PoiViewItemList poiData = null)
        {
            if (poiData is null)
            {
                if (GpsUtils.HasLocation(MyLocation))
                {
                    var poiList = Database.GetItems(MyLocation, Settings.MaxDistance);
                    PoiData = new PoiViewItemList(poiList, MyLocation, Settings.MaxDistance, Settings.MinAltitute, ShowFavoritesOnly, Settings.Categories);
                }
            }
            else
            {
                PoiData = poiData;
            }

            var args = new DataChangedEventArgs() { PoiData = PoiData };
            DataChanged?.Invoke(this, args);
        }

        protected void NotifyHeadingChanged(double heading)
        {
            var args = new HeadingChangedEventArgs() { Heading = heading };
            HeadingChanged?.Invoke(this, args);
        }

        protected virtual void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            if (e.ChangedData == ChangedData.PoiFilterSettings)
            {
                ReloadData();
            }
        }

        protected void LogError(string v, Exception ex)
        {
            //TODO: logging
        }

        public virtual void Pause()
        {
        }

        public virtual void Resume()
        {
        }

        public void SetLocale(ContextWrapper appContext)
        {
            
            switch (Settings.Language)
            {
                case Languages.English:
                    appContext.Resources.Configuration.SetLocale(new Java.Util.Locale("en"));
                    break;
                case Languages.German:
                    appContext.Resources.Configuration.SetLocale(new Java.Util.Locale("de"));
                    break;
                case Languages.Czech:
                    appContext.Resources.Configuration.SetLocale(new Java.Util.Locale("cz"));
                    break;
            }

            appContext.Resources.UpdateConfiguration(appContext.Resources.Configuration, appContext.Resources.DisplayMetrics);
        }
    }
}