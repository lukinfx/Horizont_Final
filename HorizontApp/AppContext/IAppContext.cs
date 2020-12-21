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

    public class HeadingChangedEventArgs : EventArgs { public double Heading; }
    public delegate void HeadingChangedEventHandler(object sender, HeadingChangedEventArgs e);

    //TODO: check Context.ApplicationContext

    public interface IAppContext
    {
        void Initialize(Android.Content.Context context);

        void Start();

        event DataChangedEventHandler DataChanged;
        event HeadingChangedEventHandler HeadingChanged;

        Settings Settings { get; }
        ElevationProfileData ElevationProfileData { get; set; }
        bool CompassPaused { get; set; }
        GpsLocation MyLocation { get; }
        double Heading { get; }
        PoiViewItemList PoiData { get; }
        bool IsPortrait { get; }
        PoiDatabase Database { get; }
        void ToggleCompassPaused();
        void ReloadData();
        List<ProfileLine> ListOfProfileLines { get; set; }

        void Pause();
        void Resume();

        double? ElevationProfileDataDistance { get; set; }
    }
}