using System;
using HorizontApp.DataAccess;
using HorizontLib.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontLib.Domain.Models;

namespace HorizontApp.AppContext
{
    public class AppContextBase : IAppContext
    {
        public event DataChangedEventHandler DataChanged;

        public Settings Settings { get; private set; }
        public ElevationProfileData ElevationProfileData { get; set; }

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

        protected void ReloadData()
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

        private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            Settings.SaveData();

            ReloadData();
        }

        private void LogError(string v, Exception ex)
        {
            //TODO: logging
        }
    }
}