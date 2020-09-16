﻿using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using Android.Views;
using static Android.Views.View;
using HorizontApp.Utilities;
using System;
using System.Linq;
using Android.Content;
using Android.Text;
using Java.Lang;
using Double = Java.Lang.Double;
using Exception = Java.Lang.Exception;
using Math = System.Math;
using System.Timers;
using Xamarin.Essentials;

namespace HorizontApp.Activities
{
    [Activity(Label = "SettingsActivity")]
    public class SettingsActivity : Activity, IOnClickListener
    {
        private CompassViewSettings _settings = CompassViewSettings.Instance();
        private Switch _switchManualViewAngle;
        private TextView _textManualViewAngle;
        private SeekBar _seekBarManualViewAngle;
        private Spinner _spinnerAppStyle;
        private Button _buttonBack;
        private AppStyles[] _listOfAppStyles = new AppStyles[] {AppStyles.EachPoiSeparate, AppStyles.FullScreenRectangle, AppStyles.Simple, AppStyles.SimpleWithDistance, AppStyles.SimpleWithHeight};
        private Timer _changeFilterTimer = new Timer();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            var orientation = mainDisplayInfo.Orientation;
            if (orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.SettingsActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.SettingsActivityLandscape);
            }

            _buttonBack = FindViewById<Button>(Resource.Id.buttonBack);
            _buttonBack.SetOnClickListener(this);

            _spinnerAppStyle = FindViewById<Spinner>(Resource.Id.spinnerAppStyle);
            _switchManualViewAngle = FindViewById<Switch>(Resource.Id.switchManualViewAngle);
            _seekBarManualViewAngle = FindViewById<SeekBar>(Resource.Id.seekBarManualViewAngle);
            _textManualViewAngle = FindViewById<TextView>(Resource.Id.textManualViewAngle);

            _spinnerAppStyle.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(appStyle_ItemSelected);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfAppStyles.ToList());
            _spinnerAppStyle.Adapter = adapter;
            _spinnerAppStyle.SetSelection(_listOfAppStyles.ToList().FindIndex(i => i == _settings.AppStyle));

            _switchManualViewAngle.SetOnClickListener(this);
            _switchManualViewAngle.Checked = _settings.IsManualViewAngle;

            _seekBarManualViewAngle.ProgressChanged += SeekBarProgressChanged;
            _seekBarManualViewAngle.Enabled = _settings.IsManualViewAngle;
            _seekBarManualViewAngle.Progress = (int) (_settings.ViewAngleHorizontal * 10);

            _textManualViewAngle.Text = GetViewAngleText(_settings.IsManualViewAngle, _settings.ViewAngleHorizontal);

            _changeFilterTimer.Enabled = false;
            _changeFilterTimer.Interval = 3000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (CompassViewSettings.Instance().IsManualViewAngle)
            {
                var viewAngle = _seekBarManualViewAngle.Progress / (float) 10.0;
                _textManualViewAngle.Text = GetViewAngleText(_settings.IsManualViewAngle, viewAngle);
                _changeFilterTimer.Stop();
                _changeFilterTimer.Start();
            }
        }

        private void appStyle_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _settings.AppStyle = _listOfAppStyles[e.Position];
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
                case Resource.Id.switchManualViewAngle:
                    _settings.IsManualViewAngle = _switchManualViewAngle.Checked;
                    _textManualViewAngle.Text = GetViewAngleText(_settings.IsManualViewAngle, _settings.ViewAngleHorizontal);
                    _seekBarManualViewAngle.Enabled = _settings.IsManualViewAngle;
                    _seekBarManualViewAngle.Progress = (int) (_settings.ViewAngleHorizontal * 10);
                    break;
            }
        }

        private string GetViewAngleText(bool manual, double viewAngle)
        {
            var autoOrManual = manual ? "set manually" : "obtained automatically";
            return $"Current camera view angle is {viewAngle:0.0} ({autoOrManual})";
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {

            _changeFilterTimer.Stop();
            var viewAngle = _seekBarManualViewAngle.Progress / (float) 10.0;
            _settings.ManualViewAngleHorizontal = viewAngle;
        }
    }
}