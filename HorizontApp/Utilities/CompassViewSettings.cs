using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net.Wifi.Aware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace HorizontApp.Utilities
{
    public class SettingsChangedEventArgs : EventArgs {}
    public delegate void SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);

    public sealed class CompassViewSettings
    {
        public event SettingsChangedEventHandler SettingsChanged;

        private static CompassViewSettings instance = null;

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


    }
}