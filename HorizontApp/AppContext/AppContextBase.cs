﻿using System;
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
                    PoiData = new PoiViewItemList(poiList, MyLocation, Settings.MaxDistance, Settings.MinAltitute, Settings.ShowFavoritesOnly, Settings.Categories);
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
            ReloadData();
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
    }
}