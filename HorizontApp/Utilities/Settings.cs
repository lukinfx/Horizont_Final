using Android.Content;
using Android.Preferences;
using HorizontLib.Domain.Enums;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HorizontLib.Domain.ViewModel;
using Xamarin.Essentials;

namespace HorizontApp.Utilities
{
    public class SettingsChangedEventArgs : EventArgs {}
    public delegate void SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);

    public sealed class Settings
    {
        public event SettingsChangedEventHandler SettingsChanged;

        private Context mContext;

        private Timer _changeFilterTimer = new Timer();

        public Settings()
        {
            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
        }

        private bool isManualViewAngle;
        public bool IsManualViewAngle
        {
            get
            {
                return isManualViewAngle;
            }
            set
            {
                isManualViewAngle = value;
                NotifySettingsChanged();
            }
        }

        private float manualViewAngleHorizontal;
        public float ManualViewAngleHorizontal
        {
            get
            {
                return manualViewAngleHorizontal;
            }
            set
            {
                manualViewAngleHorizontal = value;
                NotifySettingsChanged();
            }
        }

        private float? viewAngleHorizontal;
        public float ViewAngleHorizontal
        {
            get
            {
                if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Landscape)
                {
                    return (isManualViewAngle || !viewAngleHorizontal.HasValue) ? manualViewAngleHorizontal : viewAngleHorizontal.Value;
                } else
                    return viewAngleVertical;

            }
            set 
            {
                viewAngleHorizontal = value;
                NotifySettingsChanged();
            }
        }

        private float viewAngleVertical;
        public float ViewAngleVertical
        {
            get
            {
                if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Landscape)
                {
                    return viewAngleVertical;
                }
                else
                    return (isManualViewAngle || !viewAngleHorizontal.HasValue) ? manualViewAngleHorizontal : viewAngleHorizontal.Value;

            }
            set
            {
                viewAngleVertical = value;
                NotifySettingsChanged();
            }
        }

        private AppStyles appStyle = AppStyles.FullScreenRectangle;
        public AppStyles AppStyle
        {
            get  
            { 
                return appStyle; 
            }
            set 
            { 
                appStyle = value;
                NotifySettingsChanged();
            }
        }

        private List<PoiCategory> categories = new List<PoiCategory>();
        public List<PoiCategory> Categories
        {
            get
            {
                return categories;
            }
            set
            {
                categories = value;
                NotifySettingsChanged();
            }
        }

        private bool _favourite;
        public bool Favourite
        {
            get { return _favourite; }
            private set { _favourite = value; NotifySettingsChanged(); }
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

        public void NotifySettingsChanged()
        {
            var args = new SettingsChangedEventArgs();
            SettingsChanged?.Invoke(this, args);
        }

        public void LoadData(Context context)
        {
            mContext = context;

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);

            String str = prefs.GetString("AppStyle", appStyle.ToString());
            appStyle = Enum.Parse<AppStyles>(str);

            var categoriesAsCollection = prefs.GetStringSet("Categories", GetDefaultCategories());
            categories.Clear();
            foreach (var i in categoriesAsCollection)
            {
                categories.Add(Enum.Parse<PoiCategory>(i));
            }

            isManualViewAngle = prefs.GetBoolean("IsManualViewAngleHorizontal", false);
            manualViewAngleHorizontal = prefs.GetFloat("ManualViewAngleHorizontal", 60);

            _maxDistance = 12;
            _minAltitute = 0;
        }

        public void SaveData()
        {
            if (mContext != null)
            {
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
                ISharedPreferencesEditor editor = prefs.Edit();

                editor.PutString("AppStyle", appStyle.ToString());

                var categoriesAsCollection = new Collection<string>();
                foreach (var i in categories)
                {
                    categoriesAsCollection.Add(i.ToString());
                }
                editor.PutStringSet("Categories", categoriesAsCollection);

                editor.PutBoolean("IsManualViewAngleHorizontal", isManualViewAngle);
                editor.PutFloat("ManualViewAngleHorizontal", manualViewAngleHorizontal);

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

        public void ToggleFavourite()
        {
            Favourite = !Favourite;
        }

        private void RestartTimer()
        {
            _changeFilterTimer.Stop();
            _changeFilterTimer.Start();
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _changeFilterTimer.Stop();
            NotifySettingsChanged();
        }
    }
}