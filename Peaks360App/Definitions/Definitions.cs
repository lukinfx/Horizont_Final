using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peaks360App.Definitions
{
    public class BaseResultCode
    {
        public static readonly int MAIN_ACTIVITY = 100;
        public static readonly int MENU_ACTIVITY = 200;
        public static readonly int POILIST_ACTIVITY = 300;
        public static readonly int POIEDIT_ACTIVITY = 400;
        public static readonly int DOWNLOAD_ACTIVITY = 500;
        public static readonly int SETTINGS_ACTIVITY = 600;
        public static readonly int PHOTOS_ACTIVITY = 700;
        public static readonly int PHOTO_SHOW_ACTIVITY = 800;
        public static readonly int ABOUT_ACTIVITY = 900;
        public static readonly int ADDDOWNLOADEDDATA_ACTIVITY = 1000;
    }
}