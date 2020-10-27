using System.Linq;
using System.Timers;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Views;
using static Android.Views.View;
using HorizontApp.Utilities;
using Xamarin.Essentials;
using HorizontApp.AppContext;
using HorizontLib.Domain.ViewModel;

namespace HorizontApp.Activities
{
    [Activity(Label = "SettingsActivity")]
    public class SettingsActivity : Activity, IOnClickListener
    {
        private Settings _settings { get { return AppContextLiveData.Instance.Settings; } }

        private Switch _switchManualViewAngle;
        private TextView _textManualViewAngle;
        private SeekBar _seekBarManualViewAngle;
        private TextView _textManualViewAngleVertical;
        private SeekBar _seekBarManualViewAngleVertical;
        private Spinner _spinnerAppStyle;
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

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(false);

            _spinnerAppStyle = FindViewById<Spinner>(Resource.Id.spinnerAppStyle);
            _switchManualViewAngle = FindViewById<Switch>(Resource.Id.switchManualViewAngle);
            _seekBarManualViewAngle = FindViewById<SeekBar>(Resource.Id.seekBarManualViewAngle);
            _textManualViewAngle = FindViewById<TextView>(Resource.Id.textManualViewAngle);
            _seekBarManualViewAngleVertical = FindViewById<SeekBar>(Resource.Id.seekBarManualViewAngleVertical);
            _textManualViewAngleVertical = FindViewById<TextView>(Resource.Id.textManualViewAngleVertical);

            _spinnerAppStyle.ItemSelected += new System.EventHandler<AdapterView.ItemSelectedEventArgs>(appStyle_ItemSelected);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfAppStyles.ToList());
            _spinnerAppStyle.Adapter = adapter;
            _spinnerAppStyle.SetSelection(_listOfAppStyles.ToList().FindIndex(i => i == _settings.AppStyle));

            _switchManualViewAngle.SetOnClickListener(this);
            _switchManualViewAngle.Checked = _settings.IsManualViewAngle;

            _seekBarManualViewAngle.ProgressChanged += SeekBarProgressChanged;
            _seekBarManualViewAngle.Enabled = _settings.IsManualViewAngle;
            _seekBarManualViewAngle.Progress = (int) (_settings.ViewAngleHorizontal * 10);

            _textManualViewAngle.Text = GetViewAngleText(_settings.IsManualViewAngle, _settings.ViewAngleHorizontal);

            _seekBarManualViewAngleVertical.ProgressChanged += SeekBarProgressChanged;
            _seekBarManualViewAngleVertical.Enabled = _settings.IsManualViewAngle;
            _seekBarManualViewAngleVertical.Progress = (int)(_settings.ViewAngleVertical * 10);

            _textManualViewAngleVertical.Text = GetViewAngleText(_settings.IsManualViewAngle, _settings.ViewAngleVertical);

            _changeFilterTimer.Enabled = false;
            _changeFilterTimer.Interval = 3000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (AppContextLiveData.Instance.Settings.IsManualViewAngle)
            {
                var viewAngle = _seekBarManualViewAngle.Progress / (float) 10.0;
                _textManualViewAngle.Text = GetViewAngleText(_settings.IsManualViewAngle, viewAngle);

                var viewAngleVertical = _seekBarManualViewAngleVertical.Progress / (float)10.0;
                _textManualViewAngleVertical.Text = GetViewAngleText(_settings.IsManualViewAngle, viewAngleVertical);

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
                case Resource.Id.switchManualViewAngle:
                    _settings.IsManualViewAngle = _switchManualViewAngle.Checked;
                    _textManualViewAngle.Text = GetViewAngleText(_settings.IsManualViewAngle, _settings.ViewAngleHorizontal);
                    _seekBarManualViewAngle.Enabled = _settings.IsManualViewAngle;
                    _seekBarManualViewAngle.Progress = (int) (_settings.ViewAngleHorizontal * 10);

                    _textManualViewAngleVertical.Text = GetViewAngleText(_settings.IsManualViewAngle, _settings.ViewAngleVertical);
                    _seekBarManualViewAngleVertical.Enabled = _settings.IsManualViewAngle;
                    _seekBarManualViewAngleVertical.Progress = (int)(_settings.ViewAngleVertical * 10);
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


            var viewAngleVertical = _seekBarManualViewAngleVertical.Progress / (float)10.0;
            _settings.ManualViewAngleVertical = viewAngleVertical;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}