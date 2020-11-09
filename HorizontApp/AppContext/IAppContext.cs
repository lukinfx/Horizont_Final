using System;
using HorizontApp.Utilities;
using HorizontApp.DataAccess;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Domain.Models;
using System.Collections.Generic;
using HorizonLib.Domain.Models;

namespace HorizontApp.AppContext
{
    public class DataChangedEventArgs : EventArgs { public PoiViewItemList PoiData; }
    public delegate void DataChangedEventHandler(object sender, DataChangedEventArgs e);

    //TODO: check Context.ApplicationContext

    public interface IAppContext
    {
        void Start();

        event DataChangedEventHandler DataChanged;
        Settings Settings { get; }
        ElevationProfileData ElevationProfileData { get; set; }
        bool CompassPaused { get; set; }
        GpsLocation MyLocation { get; }
        double Heading { get; }
        PoiViewItemList PoiData { get; }
        PoiDatabase Database { get; }
        void ToggleCompassPaused();
        void ReloadData();
        List<ProfileLine> ListOfProfileLines { get; set; }

        void Pause();
        void Resume();
        double? ElevationProfileDataDistance { get; set; }
    }
}