using System;
using HorizontApp.Utilities;
using HorizontApp.DataAccess;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Domain.Models;

namespace HorizontApp.AppContext
{
    public class DataChangedEventArgs : EventArgs { public PoiViewItemList PoiData; }
    public delegate void DataChangedEventHandler(object sender, DataChangedEventArgs e);

    //TODO: check Context.ApplicationContext

    public interface IAppContext
    {
        event DataChangedEventHandler DataChanged;
        Settings Settings { get; }
        ElevationProfileData ElevationProfileData { get; set; }
        bool CompassPaused { get; set; }
        GpsLocation MyLocation { get; }
        double Heading { get; }
        PoiViewItemList PoiData { get; }
        PoiDatabase Database { get; }
        void ToggleCompassPaused();
    }
}