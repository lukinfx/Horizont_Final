using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Android.Content;
using Android.Preferences;
using Xamarin.Essentials;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.ViewModel;

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
            _maxDistance = 12;
            _minAltitute = 0;
            Categories.Add(PoiCategory.Mountains);

            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
        }

        private bool isViewAngleCorrection;
        public bool IsViewAngleCorrection
        {
            get
            {
                return isViewAngleCorrection;
            }
            set
            {
                isViewAngleCorrection = value;
                NotifySettingsChanged();
            }
        }

        public float? AutomaticViewAngleHorizontal { get; private set; }
        public float? AutomaticViewAngleVertical { get; private set; }

        private float correctionViewAngleHorizontal;
        public float CorrectionViewAngleHorizontal
        {
            get
            {
                return correctionViewAngleHorizontal;
            }
            set
            {
                correctionViewAngleHorizontal = value;
                NotifySettingsChanged();
            }
        }

        private float correctionViewAngleVertical;
        public float CorrectionViewAngleVertical
        {
            get
            {
                return correctionViewAngleVertical;
            }
            set
            {
                correctionViewAngleVertical = value;
                NotifySettingsChanged();
            }
        }

        
        public float ViewAngleHorizontal
        {
            get
            {
                if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Landscape)
                    return (AutomaticViewAngleHorizontal.HasValue?AutomaticViewAngleHorizontal.Value:60) + (isViewAngleCorrection?correctionViewAngleHorizontal:0);
                else
                    return (AutomaticViewAngleVertical.HasValue?AutomaticViewAngleVertical.Value:40) + (isViewAngleCorrection? correctionViewAngleVertical:0);

            }
        }

        public float ViewAngleVertical
        {
            get
            {
                if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Landscape)
                    return (AutomaticViewAngleVertical.HasValue ? AutomaticViewAngleVertical.Value : 40) + (isViewAngleCorrection ? correctionViewAngleVertical : 0);
                else
                    return (AutomaticViewAngleHorizontal.HasValue ? AutomaticViewAngleHorizontal.Value : 60) + (isViewAngleCorrection ? correctionViewAngleHorizontal : 0);
            }
        }

        private System.Drawing.Size _cameraPictureSize;
        public System.Drawing.Size CameraPictureSize { get { return _cameraPictureSize; } }

        internal void SetCameraParameters(float horizontalViewAngle, float verticalViewAngle, int imageWidth, int imageHeight)
        {
            AutomaticViewAngleHorizontal = horizontalViewAngle;
            AutomaticViewAngleVertical = verticalViewAngle;
            _cameraPictureSize = new System.Drawing.Size(imageWidth, imageHeight);
            NotifySettingsChanged();
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

            isViewAngleCorrection = prefs.GetBoolean("IsViewAngleCorrection", false);

            correctionViewAngleHorizontal = prefs.GetFloat("CorrectionViewAngleHorizontal", 0);
            correctionViewAngleVertical = prefs.GetFloat("CorrectionViewAngleVertical", 0);
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

                editor.PutBoolean("IsViewAngleCorrection", isViewAngleCorrection);
                editor.PutFloat("CorrectionViewAngleHorizontal", correctionViewAngleHorizontal);
                editor.PutFloat("CorrectionViewAngleVertical", correctionViewAngleVertical);

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