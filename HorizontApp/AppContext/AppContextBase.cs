using System;
using HorizontApp.DataAccess;
using HorizontLib.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontLib.Domain.Models;
using System.Collections.Generic;
using HorizonLib.Domain.Models;

namespace HorizontApp.AppContext
{
    public abstract class AppContextBase : IAppContext
    {
        public event DataChangedEventHandler DataChanged;

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
        public double Heading { get; protected set; }

        public PoiViewItemList PoiData { get; protected set; }

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
                if (GpsUtils.HasLocation(MyLocation))
                {
                    var poiList = Database.GetItems(MyLocation, Settings.MaxDistance);
                    PoiData = new PoiViewItemList(poiList, MyLocation, Settings.MaxDistance, Settings.MinAltitute, Settings.Favourite, Settings.Categories);
                }

                var args = new DataChangedEventArgs() {PoiData = PoiData};
                DataChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogError("Error when fetching data.", ex);
            }
        }

        protected virtual void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            ReloadData();
        }

        private void LogError(string v, Exception ex)
        {
            //TODO: logging
        }
    }
}