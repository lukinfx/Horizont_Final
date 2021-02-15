using System.Globalization;
using System.Linq;
using System.Timers;
using Android.App;
using Android.OS;
using Android.Text;
using Android.Widget;
using Android.Views;
using static Android.Views.View;
using HorizontApp.Utilities;
using Xamarin.Essentials;
using HorizontApp.AppContext;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using Java.Lang;
using HorizontApp.Views.Camera;
using System.Collections.Generic;
using Android.Util;
using Android.Content;
using Android.Content.Res;
using HorizonLib.Domain.Enums;

namespace HorizontApp.Activities
{
    [Activity(Label = "SettingsActivity")]
    public class SettingsActivity : Activity, IOnClickListener
    {
        public static int REQUEST_SHOW_SETTINGS = Definitions.BaseResultCode.SETTINGS_ACTIVITY + 0;

        public static Result RESULT_CANCELED { get { return Result.Canceled; } }
        public static Result RESULT_OK { get { return Result.Ok; } }
        public static Result RESULT_OK_AND_CLOSE_PARENT { get { return (Result)2; } }


        private Settings _settings { get { return AppContextLiveData.Instance.Settings; } }

        private Switch _switchManualViewAngle;
        private Switch _switchManualGpsLocation;
        private Switch _switchAltitudeFromElevationMap;
        private Switch _switchAutoElevationProfile;

        private EditText _editTextLatitude;
        private EditText _editTextLongitude;
        private EditText _editTextAltitude;
        private TextView _textViewLatitude;
        private TextView _textViewLongitude;
        private TextView _textViewAltitude;

        private TextView _textViewAngleHorizontal;
        private SeekBar _seekBarCorrectionViewAngleHorizontal;
        private TextView _textViewAngleVertical;
        private SeekBar _seekBarCorrectionViewAngleVertical;
        private Spinner _spinnerLanguages;
        private Spinner _spinnerPhotoResolution;
        private Button _resetButton;
        private Languages[] _listOfLanguages = new Languages[] { Languages.English, Languages.German, Languages.Czech};

        private bool _isDirty = false;
        private List<Size> _listOfCameraResolutions;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppContextLiveData.Instance.SetLocale(this);

            if (AppContextLiveData.Instance.IsPortrait)
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

            _spinnerLanguages = FindViewById<Spinner>(Resource.Id.spinnerLanguage);
            _spinnerPhotoResolution = FindViewById<Spinner>(Resource.Id.spinnerResolution);

            _switchManualViewAngle = FindViewById<Switch>(Resource.Id.switchManualViewAngle);
            _seekBarCorrectionViewAngleHorizontal = FindViewById<SeekBar>(Resource.Id.seekBarCorrectionViewAngleHorizontal);
            _textViewAngleHorizontal = FindViewById<TextView>(Resource.Id.textViewAngleHorizontal);
            _seekBarCorrectionViewAngleVertical = FindViewById<SeekBar>(Resource.Id.seekBarCorrectionViewAngleVertical);
            _textViewAngleVertical = FindViewById<TextView>(Resource.Id.textViewAngleVertical);
            _resetButton = FindViewById<Button>(Resource.Id.reset);
            _resetButton.SetOnClickListener(this);

            var adapterLanguages = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfLanguages.ToList());
            _spinnerLanguages.Adapter = adapterLanguages;
            _spinnerLanguages.SetSelection(_listOfLanguages.ToList().FindIndex(i => i == _settings.Language));
            _spinnerLanguages.ItemSelected += (sender, args) => { InvalidateOptionsMenu(); };

            _listOfCameraResolutions = CameraUtilities.GetCameraResolutions(_settings.CameraId).ToList();
            var adapterPhotoResolution = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfCameraResolutions);
            _spinnerPhotoResolution.Adapter = adapterPhotoResolution;
            var resolutionIdx = _listOfCameraResolutions.FindIndex(i => i.Equals(_settings.cameraResolutionSelected));
            _spinnerPhotoResolution.SetSelection(resolutionIdx);
            _spinnerPhotoResolution.ItemSelected += (sender, args) => { InvalidateOptionsMenu(); };

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

            _switchManualGpsLocation = FindViewById<Switch>(Resource.Id.switchManualGpsLocation);
            _switchManualGpsLocation.Checked = (_settings.ManualLocation != null);
            _switchManualGpsLocation.SetOnClickListener(this);

            _textViewLatitude = FindViewById<TextView>(Resource.Id.latitudeTitle);
            _textViewLongitude = FindViewById<TextView>(Resource.Id.longitudeTitle);
            _textViewAltitude = FindViewById<TextView>(Resource.Id.altitudeTitle);
            _editTextLatitude = FindViewById<EditText>(Resource.Id.editTextLatitude);
            _editTextLongitude = FindViewById<EditText>(Resource.Id.editTextLongitude);
            _editTextAltitude = FindViewById<EditText>(Resource.Id.editTextAltitude);
            _editTextLatitude.TextChanged += ManualGpsLocationChanged;

            EnableOrDisableGpsLocationInputs(_settings.IsManualLocation);
            InitializeGpsLocationInputs(_settings.IsManualLocation ? _settings.ManualLocation : AppContextLiveData.Instance.MyLocation);

            _switchAltitudeFromElevationMap = FindViewById<Switch>(Resource.Id.switchAltitudeFromElevationMap);
            _switchAltitudeFromElevationMap.Checked = _settings.AltitudeFromElevationMap;
            _switchAltitudeFromElevationMap.SetOnClickListener(this);

            _switchAutoElevationProfile = FindViewById<Switch>(Resource.Id.switchAutoElevationProfile);
            _switchAutoElevationProfile.Checked = _settings.AutoElevationProfile;
            _switchAutoElevationProfile.SetOnClickListener(this);
        }

        private void SetLanguage()
        {
            if (_spinnerLanguages != null)
            {
                switch (_listOfLanguages[_spinnerLanguages.SelectedItemPosition])
                {
                    case Languages.English:
                        _settings.Language = Languages.English;
                        break;
                    case Languages.German:
                        _settings.Language = Languages.German;
                        break;
                    case Languages.Czech:
                        _settings.Language = Languages.Czech;
                        break;
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            _isDirty = false;
        }
        public override void OnBackPressed()
        {
            OnClose();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.SettingsActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var menuItem = menu.GetItem(0);
            menuItem.SetVisible(IsDirty());
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnClose();
                    break;
                case Resource.Id.menu_save:
                    OnSave();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private bool IsDirty()
        {
            if (_isDirty)
                return true;

            if(!_settings.cameraResolutionSelected.Equals(_listOfCameraResolutions[_spinnerPhotoResolution.SelectedItemPosition]))
                return true;

            if (_settings.Language != _listOfLanguages[_spinnerLanguages.SelectedItemPosition])
                return true;

            return false;
        }

        private void OnClose()
        {
            if (IsDirty())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) =>
                {
                    SetResult(RESULT_CANCELED);
                    Finish();
                });
                alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) =>
                {
                });
                alert.SetMessage(Resources.GetText(Resource.String.DiscardChanges));
                var answer = alert.Show();
            }
            else
            {
                SetResult(RESULT_CANCELED);
                Finish();
            }
        }

        private void OnSave()
        {
            try
            {
                //View angle correction
                _settings.IsViewAngleCorrection = _switchManualViewAngle.Checked;

                if (_switchManualViewAngle.Checked)
                {
                    var viewAngleHorizontal = _seekBarCorrectionViewAngleHorizontal.Progress / (float)10.0;
                    _settings.CorrectionViewAngleHorizontal = viewAngleHorizontal;

                    var viewAngleVertical = _seekBarCorrectionViewAngleVertical.Progress / (float)10.0;
                    _settings.CorrectionViewAngleVertical = viewAngleVertical;
                }

                //Manual GPS location
                if (_switchManualGpsLocation.Checked)
                {
                    var loc = new GpsLocation(
                        double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture),
                        double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture),
                        double.Parse(_editTextAltitude.Text, CultureInfo.InvariantCulture));
                    _settings.ManualLocation = loc;
                    _settings.IsManualLocation = true;
                }
                else
                {
                    _settings.IsManualLocation = false;
                }

                //Auto elevation profile
                _settings.AutoElevationProfile = _switchAutoElevationProfile.Checked;

                //Altitude from elevation map
                _settings.AltitudeFromElevationMap = _switchAltitudeFromElevationMap.Checked;
                _settings.cameraResolutionSelected = _listOfCameraResolutions[_spinnerPhotoResolution.SelectedItemPosition];
                SetLanguage();

                _settings.NotifySettingsChanged(ChangedData.ViewOptions);
                AppContextLiveData.Instance.SetLocale(this);
            }
            catch(Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", "Error when saving settings. " + ex.Message);
            }

            SetResult(RESULT_OK);
            Finish();
        }

        private void SetDirty()
        {
            _isDirty = true;
            InvalidateOptionsMenu();
        }

        private void ManualGpsLocationChanged(object sender, TextChangedEventArgs e)
        {
            SetDirty();
        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (_settings.IsViewAngleCorrection)
            {
                SetDirty();

                var viewAngleHorizontal = _seekBarCorrectionViewAngleHorizontal.Progress / (float) 10.0;
                _textViewAngleHorizontal.Text = GetViewAngleText(_settings.IsViewAngleCorrection, viewAngleHorizontal, _settings.AutomaticViewAngleHorizontal);

                var viewAngleVertical = _seekBarCorrectionViewAngleVertical.Progress / (float)10.0;
                _textViewAngleVertical.Text = GetViewAngleText(_settings.IsViewAngleCorrection, viewAngleVertical, _settings.AutomaticViewAngleVertical);
            }
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.reset:
                    SetDirty();
                    _seekBarCorrectionViewAngleHorizontal.Progress = 0;
                    _seekBarCorrectionViewAngleVertical.Progress = 0;
                    break;

                case Resource.Id.switchManualViewAngle:
                    SetDirty();
                    _textViewAngleHorizontal.Text = GetViewAngleText(
                        _switchManualViewAngle.Checked, 
                        _seekBarCorrectionViewAngleHorizontal.Progress / (float)10.0, 
                        _settings.AutomaticViewAngleHorizontal);
                    _textViewAngleVertical.Text = GetViewAngleText(
                        _switchManualViewAngle.Checked, 
                        _seekBarCorrectionViewAngleVertical.Progress / (float)10.0, 
                        _settings.AutomaticViewAngleVertical);
                    _seekBarCorrectionViewAngleHorizontal.Enabled = _switchManualViewAngle.Checked;
                    _seekBarCorrectionViewAngleVertical.Enabled = _switchManualViewAngle.Checked;
                    break;
                case Resource.Id.switchManualGpsLocation:
                    SetDirty();
                    EnableOrDisableGpsLocationInputs(_switchManualGpsLocation.Checked);
                    break;
                case Resource.Id.switchAltitudeFromElevationMap:
                    SetDirty();
                    break;
                case Resource.Id.switchAutoElevationProfile:
                    SetDirty();
                    break;
            }
        }

        private void InitializeGpsLocationInputs(GpsLocation loc)
        {
            _editTextAltitude.Text = $"{loc.Altitude:F0}";
            _editTextLongitude.Text = $"{loc.Longitude:F7}".Replace(",", ".");
            _editTextLatitude.Text = $"{loc.Latitude:F7}".Replace(",", ".");
        }

        private void EnableOrDisableGpsLocationInputs(bool enabled)
        {
            _editTextLongitude.Enabled = enabled;
            _editTextLatitude.Enabled = enabled;
            _editTextAltitude.Enabled = enabled;
            _textViewLongitude.Enabled = enabled;
            _textViewLatitude.Enabled = enabled;
            _textViewAltitude.Enabled = enabled;
        }

        private string GetViewAngleText(bool manual, float? correctionViewAngle, float? automaticViewAngle)
        {
            var viewAngle = manual ? automaticViewAngle + correctionViewAngle : automaticViewAngle;
            var correction = manual ? correctionViewAngle : 0;
            return $"Correction: {correction:0.0}   View angle: {viewAngle:0.0} ()";
        }
    }
}