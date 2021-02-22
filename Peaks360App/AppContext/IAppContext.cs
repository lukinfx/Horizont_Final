﻿using System;
using Peaks360App.Utilities;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Domain.Models;
using System.Collections.Generic;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360App.Models;

namespace Peaks360App.AppContext
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
        PhotosModel PhotosModel { get; }
        ElevationProfileData ElevationProfileData { get; set; }
        bool CompassPaused { get; set; }
        GpsLocation MyLocation { get; }
        string MyLocationName { get; }
        double Heading { get; }
        double HeadingCorrector { get; set; }
        PoiViewItemList PoiData { get; }
        bool IsPortrait { get; }
        float ViewAngleHorizontal { get; }
        float ViewAngleVertical { get; }
        PoiDatabase Database { get; }
        void ToggleCompassPaused();
        void ToggleFavourite();
        void ToggleFavouritePictures();
        void ToggleFavouritePois();
        void ReloadData();
        List<ProfileLine> ListOfProfileLines { get; set; }
        bool ShowFavoritesOnly { get; set; }
        bool ShowFavoritePicturesOnly { get; set; }
        bool ShowFavoritePoisOnly { get; set; }
        PoiFilter SelectedPoiFilter { get; set; }
        PoiViewItem SelectedPoi { get; set; }
        void Pause();
        void Resume();
        void SetLocale(Android.Content.ContextWrapper contextWrapper);
    }
}