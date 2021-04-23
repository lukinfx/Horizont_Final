using System;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Content.PM;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Xamarin.Forms;
using Peaks360App.AppContext;
using Peaks360App.Services;

/*
 * 1) Request permissions
 * 2) Show Privacy Policy Statement
 * 3) Check If Gps Is Available
 * 4) Start MainActivity
 */

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/SplashScreenTheme", MainLauncher = true)]
    public class StartUpActivity : Activity
    {
        private const int REQUEST_PERMISSIONS = Definitions.BaseResultCode.STARTUP_ACTIVITY + 0;
        private const int REQUEST_GPS_SETTINGS = Definitions.BaseResultCode.STARTUP_ACTIVITY + 1;
        private const int REQUEST_PRIVACY_POLICY = Definitions.BaseResultCode.STARTUP_ACTIVITY + 2;

        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle bundle)
        {
            //base.SetTheme(Resource.Style.AppTheme);
            base.OnCreate(bundle);
            Xamarin.Forms.Forms.Init(this, bundle);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadUserDictionary) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteUserDictionary) != Permission.Granted)
            {
                RequestPermissions();
            }
            else
            {
                InitializeContext();
            }
        }

        private void RequestPermissions()
        {
            //From: https://docs.microsoft.com/cs-cz/xamarin/android/app-fundamentals/permissions
            //Sample app: https://github.com/xamarin/monodroid-samples/tree/master/android-m/RuntimePermissions

            var requiredPermissions = new String[]
            {
                Manifest.Permission.AccessFineLocation,
                Manifest.Permission.Camera,
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.ReadUserDictionary,
                Manifest.Permission.WriteUserDictionary
            };

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadExternalStorage) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.WriteExternalStorage) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadUserDictionary) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.WriteUserDictionary))
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                alert.SetPositiveButton(Resources.GetText(Android.Resource.String.Ok), (senderAlert, args) =>
                {
                    ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_PERMISSIONS);
                });
                alert.SetMessage("Internal storage, location and camera permissions are needed to show relevant data.");
                var answer = alert.Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_PERMISSIONS);
            }
        }

        private void InitializeContext()
        {
            AppContextLiveData.Instance.Initialize(this);
            AppContextLiveData.Instance.SetLocale(this);

            ShowPrivacyPolicy();
        }

        private void ShowPrivacyPolicy()
        {

            if (Context.Settings.IsPrivacyPolicyApprovementNeeded())
            {
                Intent privacyPolicyActivityIntent = new Intent(this, typeof(PrivacyPolicyActivity));
                StartActivityForResult(privacyPolicyActivityIntent, REQUEST_PRIVACY_POLICY);
            }
            else
            {
                CheckGpsAvailable();
            }
        }

        private void CheckGpsAvailable()
        {
            bool gpsAvailable = DependencyService.Get<IGpsService>().IsGpsAvailable();
            if (!gpsAvailable)
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
                {
                    //DependencyService.Get<IGpsService>().OpenSettings(this);
                    Intent intent = new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                    intent.AddFlags(ActivityFlags.BroughtToFront);
                    StartActivityForResult(intent, REQUEST_GPS_SETTINGS);
                });
                alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) =>
                {
                    StartMainActivity();
                });
                alert.SetMessage(Resources.GetText(Resource.String.Main_EnableGpsQuestion));
                var answer = alert.Show();
            }
            else
            {
                StartMainActivity();
            }
        }

        private void StartMainActivity()
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
            Finish();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            switch(requestCode)
            {
                case REQUEST_PERMISSIONS:
                    InitializeContext();
                    break;
                case REQUEST_PRIVACY_POLICY:
                    CheckGpsAvailable();
                    break;
                case REQUEST_GPS_SETTINGS:
                    StartMainActivity();
                    break;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            for (int i = 0; i < permissions.Length; i++)
            {
                if (permissions[i] == Manifest.Permission.Camera && grantResults[i] == Permission.Denied)
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                    alert.SetPositiveButton(Resources.GetText(Android.Resource.String.Ok), (senderAlert, args) => { Finish(); });
                    alert.SetMessage("The Peaks360 application is not working properly without access to camera.");
                    var answer = alert.Show();
                    return;
                }
            }

            OnActivityResult(REQUEST_PERMISSIONS, Result.Ok, null);
        }

    }
}