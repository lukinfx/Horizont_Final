﻿using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Android.Content;
using Android.Preferences;
using Xamarin.Essentials;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using Android.Util;
using HorizonLib.Domain.Enums;

namespace HorizontApp.Utilities
{
    public enum ChangedData
    {
        ViewOptions,
        PoiFilterSettings
    }


    public class SettingsChangedEventArgs : EventArgs
    {
        public SettingsChangedEventArgs(ChangedData changedData)
        : base()
        {
            ChangedData = changedData;
        }

        public ChangedData ChangedData { get; private set; }
    }
    public delegate void SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);

    public sealed class Settings
    {
        public event SettingsChangedEventHandler SettingsChanged;

        public HorizonLib.Domain.Enums.Languages Language;

        private Context mContext;

        private Timer _changeFilterTimer = new Timer();

        public Size[] cameraResolutionList;

        public Size cameraResolutionSelected { get; set; }

        public Settings()
        {
            _maxDistance = 12;
            _minAltitute = 0;
            Categories = new List<PoiCategory>();
            AppStyle = AppStyles.FullScreenRectangle;

            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
        }

        public bool IsViewAngleCorrection { get; set; }
        public float? AutomaticViewAngleHorizontal { get; private set; }
        public float? AutomaticViewAngleVertical { get; private set; }
        public float CorrectionViewAngleHorizontal { get; set; }
        public float CorrectionViewAngleVertical { get; set; }
        public bool IsManualLocation { get; set; }
        public GpsLocation ManualLocation { get; set; }
        public bool AltitudeFromElevationMap { get; set; }
        public bool AutoElevationProfile { get; set; }
        public bool ShowElevationProfile { get; set; }
        public string CameraId { get; set; }
        public AppStyles AppStyle { get; set; }
        public List<PoiCategory> Categories { get; set; }

        public float AViewAngleHorizontal
        {
            get
            {
                return (AutomaticViewAngleHorizontal.HasValue?AutomaticViewAngleHorizontal.Value:60) + (IsViewAngleCorrection?CorrectionViewAngleHorizontal:0);
            }
        }

        public float AViewAngleVertical
        {
            get
            {
                return (AutomaticViewAngleVertical.HasValue ? AutomaticViewAngleVertical.Value : 40) + (IsViewAngleCorrection ? CorrectionViewAngleVertical : 0);
            }
        }

        private System.Drawing.Size _cameraPictureSize;
        public System.Drawing.Size CameraPictureSize { get { return _cameraPictureSize; } }

        internal void SetCameraParameters(float horizontalViewAngle, float verticalViewAngle, int imageWidth, int imageHeight)
        {
            AutomaticViewAngleHorizontal = horizontalViewAngle;
            AutomaticViewAngleVertical = verticalViewAngle;
            _cameraPictureSize = new System.Drawing.Size(imageWidth, imageHeight);
            NotifySettingsChanged(ChangedData.ViewOptions);
        }

        private int _minAltitute;
        public int MinAltitute 
        {
            get { return _minAltitute; }
            set { _minAltitute = value; RestartTimer(); }
        }

        private int _maxDistance;
        public int MaxDistance
        {
            get { return _maxDistance; }
            set { _maxDistance = value; RestartTimer(); }
        }

        public void NotifySettingsChanged(ChangedData changedData)
        {
            var args = new SettingsChangedEventArgs(changedData);
            SettingsChanged?.Invoke(this, args);
        }

        public void LoadData(Context context)
        {
            mContext = context;

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);

            String str = prefs.GetString("AppStyle", AppStyle.ToString());
            AppStyle = Enum.Parse<AppStyles>(str);

            var categoriesAsCollection = prefs.GetStringSet("Categories", GetDefaultCategories());
            Categories.Clear();
            foreach (var i in categoriesAsCollection)
            {
                Categories.Add(Enum.Parse<PoiCategory>(i));
            }

            IsViewAngleCorrection = prefs.GetBoolean("IsViewAngleCorrection", false);

            CorrectionViewAngleHorizontal = prefs.GetFloat("CorrectionViewAngleHorizontal", 0);
            CorrectionViewAngleVertical = prefs.GetFloat("CorrectionViewAngleVertical", 0);

            AltitudeFromElevationMap = prefs.GetBoolean("AltitudeFromElevationMap", false);
            AutoElevationProfile = prefs.GetBoolean("AutoElevationProfile", false);

            cameraResolutionSelected = new Size (prefs.GetInt("CameraResolutionWidth", 0), prefs.GetInt("CameraResolutionHeight", 0));
            CameraId= prefs.GetString("CameraId", null);

            string lan = prefs.GetString("Language", Languages.English.ToString());
            Language = Enum.Parse<Languages>(lan);

            ShowElevationProfile = AutoElevationProfile;
        }

        public void SaveData()
        {
            if (mContext != null)
            {
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
                ISharedPreferencesEditor editor = prefs.Edit();

                editor.PutString("AppStyle", AppStyle.ToString());

                var categoriesAsCollection = new Collection<string>();
                foreach (var i in Categories)
                {
                    categoriesAsCollection.Add(i.ToString());
                }
                editor.PutStringSet("Categories", categoriesAsCollection);

                editor.PutBoolean("IsViewAngleCorrection", IsViewAngleCorrection);
                editor.PutFloat("CorrectionViewAngleHorizontal", CorrectionViewAngleHorizontal);
                editor.PutFloat("CorrectionViewAngleVertical", CorrectionViewAngleVertical);

                editor.PutBoolean("AltitudeFromElevationMap", AltitudeFromElevationMap);
                editor.PutBoolean("AutoElevationProfile", AutoElevationProfile);

                editor.PutInt("CameraResolutionWidth", cameraResolutionSelected.Width);
                editor.PutInt("CameraResolutionHeight", cameraResolutionSelected.Height);
                editor.PutString("CameraId", CameraId);
                editor.PutString("Language", Language.ToString());
                
                editor.Apply();
            }
        }

        private ICollection<string> GetDefaultCategories()
        {
            var categoriesAsCollectionDefault = new Collection<string>
            {
                PoiCategory.Mountains.ToString(),
                PoiCategory.Lakes.ToString(),
                PoiCategory.Castles.ToString(),
                PoiCategory.Palaces.ToString(),
                PoiCategory.Ruins.ToString(),
                PoiCategory.ViewTowers.ToString(),
                PoiCategory.Transmitters.ToString(),
                PoiCategory.Churches.ToString()
            };

            return categoriesAsCollectionDefault;
        }

        private void RestartTimer()
        {
            _changeFilterTimer.Stop();
            _changeFilterTimer.Start();
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _changeFilterTimer.Stop();
            NotifySettingsChanged(ChangedData.PoiFilterSettings);
        }
    }
}