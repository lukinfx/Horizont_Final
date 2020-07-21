﻿using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using Android.Views;
using Android.Content;
using static Android.Views.View;
using HorizontApp.Utilities;
using Xamarin.Essentials;
using System;
using Java.Interop;
using Android.Database;
using Android.Support.V7.Widget;
using System.Collections.Generic;
using System.Linq;

namespace HorizontApp.Activities
{



    [Activity(Label = "SettingsActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class SettingsActivity : Activity, IOnClickListener
    {
        CompassViewSettings settings = CompassViewSettings.Instance();
        Switch fullscreenBackground;
        Switch textBackground;
        Spinner appStyle;
        private AppStyles[] x = new AppStyles[] { AppStyles.NewStyle, AppStyles.OldStyle };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SettingsLayout);

            var fullscreenBackground = FindViewById<Switch>(Resource.Id.TransparentRectangleFullscreen);
            fullscreenBackground.SetOnClickListener(this);

            var appStyle = FindViewById<Spinner>(Resource.Id.spinnerAppStyle);
            //var appStyleDropDown = FindViewById<DropDownListView>(Resource.Id.appStyleDropDown);
            

            var textBackground = FindViewById<Switch>(Resource.Id.TransparentRectangleTextBackground);
            textBackground.SetOnClickListener(this);

            appStyle.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(appStyle_ItemSelected);
            //var adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.SpinnerItemsArray, Android.Resource.Layout.SimpleSpinnerItem);
            //adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, x.ToList());
            appStyle.Adapter = adapter;
            appStyle.SetSelection(x.ToList().FindIndex(i => i == CompassViewSettings.Instance().AppStyle));
        }

        private void appStyle_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            CompassViewSettings.Instance().AppStyle = x[e.Position];
        }

        public void OnClick(View v)
        {
           
        }
    }
}