using Android.App;
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

namespace HorizontApp.Activities
{
    [Activity(Label = "SettingsActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class SettingsActivity : Activity, IOnClickListener
    {
        private CompassViewSettings settings = CompassViewSettings.Instance();
        private Switch switchManualViewAngle;
        private TextView textManualViewAngle;
        private SeekBar seekBarManualViewAngle;
        private Spinner appStyle;
        private Button back;
        private AppStyles[] _listOfAppStyles = new AppStyles[] {AppStyles.NewStyle, AppStyles.OldStyle, AppStyles.RightOnly};
        private Timer changeFilterTimer = new Timer();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SettingsLayout);

            back = FindViewById<Button>(Resource.Id.buttonBack);
            back.SetOnClickListener(this);

            appStyle = FindViewById<Spinner>(Resource.Id.spinnerAppStyle);
            switchManualViewAngle = FindViewById<Switch>(Resource.Id.switchManualViewAngle);
            seekBarManualViewAngle = FindViewById<SeekBar>(Resource.Id.seekBarManualViewAngle);
            textManualViewAngle = FindViewById<TextView>(Resource.Id.textManualViewAngle);

            appStyle.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(appStyle_ItemSelected);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfAppStyles.ToList());
            appStyle.Adapter = adapter;
            appStyle.SetSelection(_listOfAppStyles.ToList().FindIndex(i => i == settings.AppStyle));

            switchManualViewAngle.SetOnClickListener(this);
            switchManualViewAngle.Checked = settings.IsManualViewAngle;

            seekBarManualViewAngle.ProgressChanged += SeekBarProgressChanged;
            seekBarManualViewAngle.Enabled = settings.IsManualViewAngle;
            seekBarManualViewAngle.Progress = (int) (settings.ViewAngleHorizontal * 10);

            textManualViewAngle.Text = GetViewAngleText(settings.IsManualViewAngle, settings.ViewAngleHorizontal);

            changeFilterTimer.Enabled = false;
            changeFilterTimer.Interval = 3000;
            changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            changeFilterTimer.AutoReset = false;
        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (CompassViewSettings.Instance().IsManualViewAngle)
            {
                var viewAngle = seekBarManualViewAngle.Progress / (float) 10.0;
                textManualViewAngle.Text = GetViewAngleText(settings.IsManualViewAngle, viewAngle);
                changeFilterTimer.Stop();
                changeFilterTimer.Start();
            }
        }

        private void appStyle_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            settings.AppStyle = _listOfAppStyles[e.Position];
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
                case Resource.Id.switchManualViewAngle:
                    settings.IsManualViewAngle = switchManualViewAngle.Checked;
                    textManualViewAngle.Text = GetViewAngleText(settings.IsManualViewAngle, settings.ViewAngleHorizontal);
                    seekBarManualViewAngle.Enabled = settings.IsManualViewAngle;
                    seekBarManualViewAngle.Progress = (int) (settings.ViewAngleHorizontal * 10);
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

            changeFilterTimer.Stop();
            var viewAngle = seekBarManualViewAngle.Progress / (float) 10.0;
            settings.ManualViewAngleHorizontal = viewAngle;
        }
    }
}