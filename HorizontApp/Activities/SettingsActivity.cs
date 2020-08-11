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
        private AppStyles[] _listOfAppStyles = new AppStyles[] { AppStyles.NewStyle, AppStyles.OldStyle };

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
            appStyle.SetSelection(_listOfAppStyles.ToList().FindIndex(i => i == CompassViewSettings.Instance().AppStyle));

            switchManualViewAngle.SetOnClickListener(this);
            switchManualViewAngle.Checked = CompassViewSettings.Instance().IsManualViewAngle;

            seekBarManualViewAngle.ProgressChanged += SeekBarProgressChanged;
            seekBarManualViewAngle.Enabled = CompassViewSettings.Instance().IsManualViewAngle;
            seekBarManualViewAngle.Progress = (int)(CompassViewSettings.Instance().ViewAngleHorizontal*10);

            textManualViewAngle.Text = GetViewAngleText();
        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (CompassViewSettings.Instance().IsManualViewAngle)
            {
                var viewAngle = seekBarManualViewAngle.Progress/(float)10.0;
                textManualViewAngle.Text = GetViewAngleText();
                CompassViewSettings.Instance().ManualViewAngleHorizontal = viewAngle;
            }
        }

        private void appStyle_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            CompassViewSettings.Instance().AppStyle = _listOfAppStyles[e.Position];
        }

        public void OnClick(View v)
        {
           switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
                case Resource.Id.switchManualViewAngle:
                    CompassViewSettings.Instance().IsManualViewAngle = switchManualViewAngle.Checked;
                    textManualViewAngle.Text = GetViewAngleText();
                    seekBarManualViewAngle.Enabled = CompassViewSettings.Instance().IsManualViewAngle;
                    seekBarManualViewAngle.Progress = (int)(CompassViewSettings.Instance().ViewAngleHorizontal * 10);
                    break;
            }
        }

        private string GetViewAngleText()
        {
            var autoOrManual = CompassViewSettings.Instance().IsManualViewAngle ? "set manually":"obtained automatically";
            return $"Current camera view angle is {CompassViewSettings.Instance().ViewAngleHorizontal:0.0} ({autoOrManual})";
        }
    }
}