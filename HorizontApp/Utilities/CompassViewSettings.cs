using System;
using Android.Content;
using Android.Preferences;

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

        private AppStyles appStyle = AppStyles.OldStyle;
        public AppStyles AppStyle
        {
            get 
            { 
                return appStyle; 
            }
            set 
            { 
                appStyle = value;
                var args = new SettingsChangedEventArgs();
                SettingsChanged?.Invoke(this, args);

                SaveData();
            }
        }


        public static CompassViewSettings Instance()
        {
            if (instance == null)
            {
                instance = new CompassViewSettings();
                instance.appStyle = AppStyles.NewStyle;
            }
            return instance;
        }

        internal void LoadData(Context context)
        {
            mContext = context;

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);

            String str = prefs.GetString("AppStyle", appStyle.ToString());
            appStyle = Enum.Parse<AppStyles>(str);
        }

        internal void SaveData()
        {
            if (mContext != null)
            {
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
                ISharedPreferencesEditor editor = prefs.Edit();

                editor.PutString("AppStyle", appStyle.ToString());

                editor.Apply();
            }
        }
    }
}