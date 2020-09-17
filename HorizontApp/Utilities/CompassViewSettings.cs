using Android.Content;
using Android.Preferences;
using HorizontApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Essentials;

namespace HorizontApp.Utilities
{
    public class SettingsChangedEventArgs : EventArgs {}
    public delegate void SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);

    public sealed class CompassViewSettings
    {
        public event SettingsChangedEventHandler SettingsChanged;

        private static CompassViewSettings instance = null;
        private Context mContext;

        private CompassViewSettings()
        {
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
                HandleSettingsChanged();
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
                HandleSettingsChanged();
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
                HandleSettingsChanged();
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
                HandleSettingsChanged();
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
                HandleSettingsChanged();
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
                HandleSettingsChanged();
            }
        }


        public void HandleSettingsChanged()
        {
            var args = new SettingsChangedEventArgs();
            SettingsChanged?.Invoke(this, args);

            SaveData();
        }
        

        public static CompassViewSettings Instance()
        {
            if (instance == null)
            {
                instance = new CompassViewSettings();
                instance.appStyle = AppStyles.EachPoiSeparate;
            }
            return instance;
        }

        internal void LoadData(Context context)
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
        }

        internal void SaveData()
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

        ICollection<string> GetDefaultCategories()
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
                PoiCategory.Test.ToString(),
                PoiCategory.Churches.ToString()
            };

            return categoriesAsCollectionDefault;
        }
    }
}