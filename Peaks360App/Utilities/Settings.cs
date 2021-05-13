using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Android.Util;
using Android.Content;
using Android.Preferences;
using Peaks360App.Services;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Xamarin.Forms;

namespace Peaks360App.Utilities
{
    public enum ChangedData
    {
        ViewOptions,
        PoiFilterSettings,
        GpsLocation
    }

    public enum TutorialPart
    {
        MainActivity,
        PhotoEditActivity,
        PhotoShowActivity,
    }

    public class SettingsChangedEventArgs : EventArgs
    {
        public SettingsChangedEventArgs(ChangedData changedData)
        : base()
        {
            ChangedData = changedData;
        }

        public ChangedData ChangedData { get; private set; }
    }

    public delegate void SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);

    public sealed class Settings
    {
        private Context _context;

        private Timer _changeFilterTimer = new Timer();

        public event SettingsChangedEventHandler SettingsChanged;

        public Language Language;

        public Android.Util.Size CameraResolutionSelected { get; set; }

        public Settings()
        {
            _maxDistance = 12;
            _minAltitute = 0;
            Categories = new List<PoiCategory>();

            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;

            tutorialNeeded=new Dictionary<TutorialPart, bool>();
            tutorialNeeded.Add(TutorialPart.MainActivity, true);
            tutorialNeeded.Add(TutorialPart.PhotoEditActivity, true);
            tutorialNeeded.Add(TutorialPart.PhotoShowActivity, true);
        }

        public bool IsViewAngleCorrection { get; set; }
        public float? AutomaticViewAngleHorizontal { get; private set; }
        public float? AutomaticViewAngleVertical { get; private set; }
        public float CorrectionViewAngleHorizontal { get; set; }
        public float CorrectionViewAngleVertical { get; set; }

        public bool IsManualLocation { get; private set; }
        public GpsLocation ManualLocation { get; private set; }
        public bool AltitudeFromElevationMap { get; set; }
        public bool AutoElevationProfile { get; set; }
        public bool ShowElevationProfile { get; set; }
        public string CameraId { get; set; }
        public List<PoiCategory> Categories { get; set; }

        public float AViewAngleHorizontal
        {
            get
            {
                return (AutomaticViewAngleHorizontal.HasValue?AutomaticViewAngleHorizontal.Value:60) + (IsViewAngleCorrection?CorrectionViewAngleHorizontal:0);
            }
        }

        public float AViewAngleVertical
        {
            get
            {
                return (AutomaticViewAngleVertical.HasValue ? AutomaticViewAngleVertical.Value : 40) + (IsViewAngleCorrection ? CorrectionViewAngleVertical : 0);
            }
        }

        private System.Drawing.Size _cameraPictureSize;
        public System.Drawing.Size CameraPictureSize { get { return _cameraPictureSize; } }

        internal void SetCameraParameters(float horizontalViewAngle, float verticalViewAngle, int imageWidth, int imageHeight)
        {
            AutomaticViewAngleHorizontal = horizontalViewAngle;
            AutomaticViewAngleVertical = verticalViewAngle;
            _cameraPictureSize = new System.Drawing.Size(imageWidth, imageHeight);
            NotifySettingsChanged(ChangedData.ViewOptions);
        }

        private int _minAltitute;
        /// <summary>
        /// Minimum altitude of the displayed points in meters
        /// </summary>
        public int MinAltitute 
        {
            get { return _minAltitute; }
            set { _minAltitute = value; RestartTimer(); }
        }

        private int _maxDistance;

        /// <summary>
        /// Max distance (visibility) in km
        /// </summary>
        public int MaxDistance // in km
        {
            get { return _maxDistance; }
            set { _maxDistance = value; RestartTimer(); }
        }

        public int PrivacyPolicyApprovementLevel = 0;
        private static int PrivacyPolicyApprovementLevelRequired = 1;

        private Dictionary<TutorialPart, bool> tutorialNeeded;

        public bool IsTutorialNeeded(TutorialPart tp)
        {
            return tutorialNeeded[tp];
        }

        public void SetTutorialNeeded(TutorialPart tp, bool value)
        {
            tutorialNeeded[tp] = value;
        }

        public bool IsPrivacyPolicyApprovementNeeded()
        {
            return PrivacyPolicyApprovementLevel < PrivacyPolicyApprovementLevelRequired;
        }

        public void PrivacyPolicyApproved()
        {
            PrivacyPolicyApprovementLevel = PrivacyPolicyApprovementLevelRequired;
            SaveData();
        }

        private bool _applicationRatingCompleted;
        private DateTime? _applicationRatingQuestionDate;

        public bool IsReviewRequired()
        {
            if (_applicationRatingCompleted)
            {
                return false;
            }

            if (!_applicationRatingQuestionDate.HasValue)
            {
                var firstInstall = DependencyService.Get<IAppVersionService>().GetInstallDate();
                _applicationRatingQuestionDate = firstInstall;
            }

            if (DateTime.Now.Subtract(_applicationRatingQuestionDate.Value).Days < 10)
            {
                return false;
            }

            _applicationRatingQuestionDate = DateTime.Now;
            
            SaveData();
            return true;
        }

        public void SetApplicationRatingCompleted()
        {
            _applicationRatingCompleted = true;
            SaveData();
        }

        public void SetManualLocation(GpsLocation location)
        {
            IsManualLocation = true;
            ManualLocation = location;
            NotifySettingsChanged(ChangedData.GpsLocation);
        }

        public void SetAutoLocation()
        {
            IsManualLocation = false;
            NotifySettingsChanged(ChangedData.GpsLocation);
        }

        public void NotifySettingsChanged(ChangedData changedData)
        {
            var args = new SettingsChangedEventArgs(changedData);
            SettingsChanged?.Invoke(this, args);
        }

        public void LoadData(Context context)
        {
            _context = context;

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);

            var categoriesAsCollection = prefs.GetStringSet("Categories", GetDefaultCategories());
            Categories.Clear();
            foreach (var i in categoriesAsCollection)
            {
                Categories.Add(Enum.Parse<PoiCategory>(i));
            }

            IsViewAngleCorrection = prefs.GetBoolean("IsViewAngleCorrection", false);

            CorrectionViewAngleHorizontal = prefs.GetFloat("CorrectionViewAngleHorizontal", 0);
            CorrectionViewAngleVertical = prefs.GetFloat("CorrectionViewAngleVertical", 0);

            AltitudeFromElevationMap = prefs.GetBoolean("AltitudeFromElevationMap", true);
            AutoElevationProfile = prefs.GetBoolean("AutoElevationProfile", true);

            PrivacyPolicyApprovementLevel = prefs.GetInt("PrivacyPolicyApprovementLevel", 0);

            {
                var isTutorialNeeded = prefs.GetBoolean("ShowTutorialMainActivity", true);
                SetTutorialNeeded(TutorialPart.MainActivity, isTutorialNeeded);
            }
            {
                var isTutorialNeeded = prefs.GetBoolean("ShowTutorialPhotoEditActivity", true);
                SetTutorialNeeded(TutorialPart.PhotoEditActivity, isTutorialNeeded);
            }
            {
                var isTutorialNeeded = prefs.GetBoolean("ShowTutorialPhotoShowActivity", true);
                SetTutorialNeeded(TutorialPart.PhotoShowActivity, isTutorialNeeded);
            }

            CameraResolutionSelected = new Android.Util.Size(prefs.GetInt("CameraResolutionWidth", 0), prefs.GetInt("CameraResolutionHeight", 0));
            CameraId= prefs.GetString("CameraId", null);

            string lan = prefs.GetString("Language", "");
            if (!Enum.TryParse<Language>(lan, out Language))
            {
                Language = PoiCountryHelper.GetDefaultLanguage();
            }

            _applicationRatingCompleted = prefs.GetBoolean("ApplicationRatingCompleted", false);
            string applicationRatingQuestionDateAsString = prefs.GetString("ApplicationRatingQuestionDate", null);
            if (DateTime.TryParse(applicationRatingQuestionDateAsString, out var applicationRatingQuestionDateAsDate))
            {
                _applicationRatingQuestionDate = applicationRatingQuestionDateAsDate;
            }


            ShowElevationProfile = AutoElevationProfile;
        }

        public void SaveData()
        {
            if (_context != null)
            {
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(_context);
                ISharedPreferencesEditor editor = prefs.Edit();

                var categoriesAsCollection = new Collection<string>();
                foreach (var i in Categories)
                {
                    categoriesAsCollection.Add(i.ToString());
                }
                editor.PutStringSet("Categories", categoriesAsCollection);

                editor.PutBoolean("IsViewAngleCorrection", IsViewAngleCorrection);
                editor.PutFloat("CorrectionViewAngleHorizontal", CorrectionViewAngleHorizontal);
                editor.PutFloat("CorrectionViewAngleVertical", CorrectionViewAngleVertical);

                editor.PutBoolean("AltitudeFromElevationMap", AltitudeFromElevationMap);
                editor.PutBoolean("AutoElevationProfile", AutoElevationProfile);

                editor.PutInt("PrivacyPolicyApprovementLevel", PrivacyPolicyApprovementLevel);

                editor.PutBoolean("ShowTutorialMainActivity", IsTutorialNeeded(TutorialPart.MainActivity));
                editor.PutBoolean("ShowTutorialPhotoEditActivity", IsTutorialNeeded(TutorialPart.PhotoEditActivity));
                editor.PutBoolean("ShowTutorialPhotoShowActivity", IsTutorialNeeded(TutorialPart.PhotoShowActivity));

                editor.PutInt("CameraResolutionWidth", CameraResolutionSelected.Width);
                editor.PutInt("CameraResolutionHeight", CameraResolutionSelected.Height);
                editor.PutString("CameraId", CameraId);
                editor.PutString("Language", Language.ToString());

                editor.PutBoolean("ApplicationRatingCompleted", _applicationRatingCompleted);
                editor.PutString("ApplicationRatingQuestionDate", _applicationRatingQuestionDate?.ToString());

                editor.Apply();
            }
        }

        private ICollection<string> GetDefaultCategories()
        {
            var categoriesAsCollectionDefault = new Collection<string>
            {
                PoiCategory.Mountains.ToString(),
                PoiCategory.Lakes.ToString(),
                PoiCategory.ViewTowers.ToString(),
                PoiCategory.Transmitters.ToString(),
                PoiCategory.Churches.ToString(),
                PoiCategory.Cities.ToString(),
                PoiCategory.Historic.ToString(),
                PoiCategory.Other.ToString()
            };

            return categoriesAsCollectionDefault;
        }

        private void RestartTimer()
        {
            _changeFilterTimer.Stop();
            _changeFilterTimer.Start();
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _changeFilterTimer.Stop();
            NotifySettingsChanged(ChangedData.PoiFilterSettings);
        }
    }
}