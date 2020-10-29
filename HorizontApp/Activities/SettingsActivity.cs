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
        private TextView _textViewAngleHorizontal;
        private SeekBar _seekBarCorrectionViewAngleHorizontal;
        private TextView _textViewAngleVertical;
        private SeekBar _seekBarCorrectionViewAngleVertical;
        private Spinner _spinnerAppStyle;
        private Button _resetButton;
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
            _seekBarCorrectionViewAngleHorizontal = FindViewById<SeekBar>(Resource.Id.seekBarCorrectionViewAngleHorizontal);
            _textViewAngleHorizontal = FindViewById<TextView>(Resource.Id.textViewAngleHorizontal);
            _seekBarCorrectionViewAngleVertical = FindViewById<SeekBar>(Resource.Id.seekBarCorrectionViewAngleVertical);
            _textViewAngleVertical = FindViewById<TextView>(Resource.Id.textViewAngleVertical);
            _resetButton = FindViewById<Button>(Resource.Id.reset);
            _resetButton.SetOnClickListener(this);

            _spinnerAppStyle.ItemSelected += new System.EventHandler<AdapterView.ItemSelectedEventArgs>(appStyle_ItemSelected);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfAppStyles.ToList());
            _spinnerAppStyle.Adapter = adapter;
            _spinnerAppStyle.SetSelection(_listOfAppStyles.ToList().FindIndex(i => i == _settings.AppStyle));

            _switchManualViewAngle.Checked = _settings.IsViewAngleCorrection;
            _switchManualViewAngle.SetOnClickListener(this);

            _seekBarCorrectionViewAngleHorizontal.Enabled = _settings.IsViewAngleCorrection;
            _seekBarCorrectionViewAngleHorizontal.Progress = (int) (_settings.CorrectionViewAngleHorizontal * 10);
            _seekBarCorrectionViewAngleHorizontal.ProgressChanged += SeekBarProgressChanged;

            _seekBarCorrectionViewAngleVertical.Enabled = _settings.IsViewAngleCorrection;
            _seekBarCorrectionViewAngleVertical.Progress = (int)(_settings.CorrectionViewAngleVertical * 10);
            _seekBarCorrectionViewAngleVertical.ProgressChanged += SeekBarProgressChanged;

            _textViewAngleHorizontal.Text = GetViewAngleText(_settings.IsViewAngleCorrection, _settings.CorrectionViewAngleHorizontal, _settings.AutomaticViewAngleHorizontal);
            _textViewAngleVertical.Text = GetViewAngleText(_settings.IsViewAngleCorrection, _settings.CorrectionViewAngleVertical, _settings.AutomaticViewAngleVertical);

            _changeFilterTimer.Enabled = false;
            _changeFilterTimer.Interval = 3000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (_settings.IsViewAngleCorrection)
            {
                var viewAngleHorizontal = _seekBarCorrectionViewAngleHorizontal.Progress / (float) 10.0;
                _textViewAngleHorizontal.Text = GetViewAngleText(_settings.IsViewAngleCorrection, viewAngleHorizontal, _settings.AutomaticViewAngleHorizontal);

                var viewAngleVertical = _seekBarCorrectionViewAngleVertical.Progress / (float)10.0;
                _textViewAngleVertical.Text = GetViewAngleText(_settings.IsViewAngleCorrection, viewAngleVertical, _settings.AutomaticViewAngleVertical);

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
                case Resource.Id.reset:
                    _seekBarCorrectionViewAngleHorizontal.Progress = 0;
                    _seekBarCorrectionViewAngleVertical.Progress = 0;
                    break;

                case Resource.Id.switchManualViewAngle:
                    _settings.IsViewAngleCorrection = _switchManualViewAngle.Checked;

                    _textViewAngleHorizontal.Text = GetViewAngleText(_settings.IsViewAngleCorrection, _settings.CorrectionViewAngleHorizontal, _settings.AutomaticViewAngleHorizontal);
                    _seekBarCorrectionViewAngleHorizontal.Enabled = _settings.IsViewAngleCorrection;
                    
                    _seekBarCorrectionViewAngleHorizontal.Progress = (int)((_settings.IsViewAngleCorrection?_settings.CorrectionViewAngleHorizontal:0) * 10);

                    _textViewAngleVertical.Text = GetViewAngleText(_settings.IsViewAngleCorrection, _settings.CorrectionViewAngleVertical, _settings.AutomaticViewAngleVertical);
                    _seekBarCorrectionViewAngleVertical.Enabled = _settings.IsViewAngleCorrection;
                    _seekBarCorrectionViewAngleVertical.Progress = (int)((_settings.IsViewAngleCorrection?_settings.CorrectionViewAngleVertical:0) * 10);
                    break;
            }
        }

        private string GetViewAngleText(bool manual, float? correctionViewAngle, float? automaticViewAngle)
        {
            var viewAngle = manual ? automaticViewAngle + correctionViewAngle : automaticViewAngle;
            var correction = manual ? correctionViewAngle : 0;
            return $"Correction: {correction:0.0}   View angle: {viewAngle:0.0} ()";
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _changeFilterTimer.Stop();
            if (_settings.IsViewAngleCorrection)
            {
                var viewAngleHorizontal = _seekBarCorrectionViewAngleHorizontal.Progress / (float) 10.0;
                _settings.CorrectionViewAngleHorizontal = viewAngleHorizontal;


                var viewAngleVertical = _seekBarCorrectionViewAngleVertical.Progress / (float) 10.0;
                _settings.CorrectionViewAngleVertical = viewAngleVertical;
            }
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