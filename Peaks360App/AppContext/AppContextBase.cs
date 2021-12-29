using System;
using System.Collections.Generic;
using Android.Content;
using Xamarin.Essentials;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.ViewModel;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360App.Models;
using Peaks360Lib.Utilities;
using GpsUtils = Peaks360App.Utilities.GpsUtils;

namespace Peaks360App.AppContext
{
    public abstract class AppContextBase : IAppContext
    {
        protected Context context;
        protected IGpsUtilities iGpsUtilities = new GpsUtilities();

        public virtual void Initialize(Context context)
        {
            this.context = context;
            PhotosModel = new PhotosModel(Database);
            DownloadedElevationDataModel = new DownloadedElevationDataModel(Database);
        }

        public event DataChangedEventHandler DataChanged;
        public event HeadingChangedEventHandler HeadingChanged;

        public Settings Settings { get; private set; }

        public PhotosModel PhotosModel { get; private set; }
        public DownloadedElevationDataModel DownloadedElevationDataModel { get; private set; }

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
            }
        }

        public bool CompassPaused { get; private set; }
        public bool DisplayOverlapped { get; private set; }

        protected GpsLocation myLocation = new GpsLocation();
        public GpsLocation MyLocation { get { return myLocation; } }

        protected PlaceInfo myLocationPlaceInfo = new PlaceInfo();
        public PlaceInfo MyLocationPlaceInfo { get { return myLocationPlaceInfo; } }

        public virtual double? HeadingX { get; protected set; }

        private double _headingCorrector = 0;
        public double HeadingCorrector
        {
            get
            {
                return _headingCorrector;
            }
            set
            {
                _headingCorrector = GpsUtils.Normalize180(value);
                NotifyHeadingChanged(HeadingX ?? 0, HeadingCorrector);
            }
        }

        public double LeftTiltCorrector { get; set; }
        public double RightTiltCorrector { get; set; }

        public bool ShowFavoritesOnly { get; set; }
        public bool ShowFavoritePicturesOnly { get; set; }
        public bool ShowFavoritePoisOnly { get; set; }
        
        public PoiSorting PoiSorting { get; set; }
        public PoiFilter SelectedPoiFilter { get; set; }
        public PoiViewItem SelectedPoi { get; set; }
        public PoiViewItemList PoiData { get; protected set; }
        public PhotosItemAdapter PhotosItemAdapter { get; set; }

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

        public void ToggleCompassPaused()
        {
            CompassPaused = !CompassPaused;

            if (CompassPaused)
            {
                StopProviders();
            }
            else
            {
                StartProviders();
            }
        }

        public void ToggleDisplayOverlapped()
        {
            DisplayOverlapped = !DisplayOverlapped;
        }

        public void ToggleFavourite()
        {
            ShowFavoritesOnly = !ShowFavoritesOnly;
        }

        public void ToggleFavouritePictures()
        {
            ShowFavoritePicturesOnly = !ShowFavoritePicturesOnly;
        }

        public void ToggleFavouritePois()
        {
            ShowFavoritePoisOnly = !ShowFavoritePoisOnly;
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
            if (GpsUtils.HasLocation(MyLocation))
            {
                if (poiData is null)
                {
                    var poiList = Database.GetItems(MyLocation, Settings.MaxDistance);
                    PoiData = new PoiViewItemList(poiList, MyLocation, Settings.MaxDistance, Settings.Categories, iGpsUtilities);
                }
                else
                {
                    PoiData = poiData;
                }

                //fetch selected point again
                if (SelectedPoi != null)
                {
                    var selectedPoi = PoiData.Find(x => x.Poi.Id == SelectedPoi.Poi.Id);
                    if (selectedPoi != null)
                    {
                        selectedPoi.Selected = true;
                        SelectedPoi = selectedPoi;
                    }
                    else
                    {
                        SelectedPoi = null;
                    }
                }

                var args = new DataChangedEventArgs() { PoiData = PoiData };
                DataChanged?.Invoke(this, args);
            }
            else
            {
                var args = new DataChangedEventArgs() { PoiData = null };
                DataChanged?.Invoke(this, args);
            }
        }

        protected void NotifyHeadingChanged(double heading, double headingCorrection)
        {
            var args = new HeadingChangedEventArgs() { Heading = heading, HeadingCorrection = headingCorrection };
            HeadingChanged?.Invoke(this, args);
        }

        protected virtual void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            switch(e.ChangedData)
            {
                case ChangedData.GpsLocation:
                case ChangedData.PoiFilterSettings:
                    ReloadData();
                    break;
            }
        }

        protected void LogError(string v, Exception ex)
        {
            //TODO: logging
        }

        public virtual void Pause()
        {
            if (!CompassPaused)
            {
                StopProviders();
            }
        }

        public virtual void Resume()
        {
            if (!CompassPaused)
            {
                StartProviders();
            }
        }

        protected virtual void StartProviders()
        {
        }

        protected virtual void StopProviders()
        {
        }

        public void SetLocale(Context appContext)
        {
            PoiCountryHelper.SetLocale(appContext, Settings.Language);
        }
    }
}