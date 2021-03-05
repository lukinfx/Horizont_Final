﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peaks360Lib.Domain.Models;
using Xamarin.Essentials;

namespace Peaks360App.Utilities
{
    public class WikiUtilities
    {
        public static bool HasWiki(Poi poi)
        {
            return !string.IsNullOrEmpty(poi.Wikipedia) || !string.IsNullOrEmpty(poi.Wikidata);
        }

        public static void OpenWiki(Poi poi)
        {
            if (!string.IsNullOrEmpty(poi.Wikipedia))
            {
                Browser.OpenAsync("https://en.wikipedia.org/w/index.php?search=" + poi.Wikipedia, BrowserLaunchMode.SystemPreferred);
            }
            if (!string.IsNullOrEmpty(poi.Wikidata))
            {
                Browser.OpenAsync("https://www.wikidata.org/wiki/" + poi.Wikidata, BrowserLaunchMode.SystemPreferred);
            }
        }

    }
}