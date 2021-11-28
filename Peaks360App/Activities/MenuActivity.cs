using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Runtime;
using Xamarin.Forms;
using Peaks360Lib.Domain.Models;
using Peaks360App.Views.ListOfPoiView;
using Peaks360App.AppContext;
using Peaks360App.Services;
using Xamarin.Essentials;
using static Android.Views.View;
using View = Android.Views.View;

namespace Peaks360App.Activities
{
    [Activity(Label = "MenuActivity")]
    public class MenuActivity : Activity, IOnClickListener
    {
        private GpsLocation _location;
        private int _maxDistance;
        private int _minAltitude;

        protected override void OnResume()
        {
            base.OnResume();
            AppContextLiveData.Instance.SetLocale(this);

            UpdatePoiCount();
        }

        protected override void OnStart()
        {
            base.OnStart();

            UpdatePoiCount();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            _location = new GpsLocation()
            {
                Latitude = Intent.GetDoubleExtra("latitude", 0),
                Longitude = Intent.GetDoubleExtra("longitude", 0),
                Altitude = Intent.GetDoubleExtra("altitude", 0),
            };

            _maxDistance = Intent.GetIntExtra("maxDistance", 0);
            _minAltitude = Intent.GetIntExtra("minAltitude", 0);

            if (AppContextLiveData.Instance.IsPortrait)
            {
                SetContentView(Resource.Layout.MenuActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.MenuActivityLandspace);
            }

            var buttonHome = FindViewById<LinearLayout>(Resource.Id.cameraLinearLayout);
            buttonHome.SetOnClickListener(this);

            var buttonList = FindViewById<LinearLayout>(Resource.Id.listOfPoisLinearLayout);
            buttonList.SetOnClickListener(this);

            var buttonDownload = FindViewById<LinearLayout>(Resource.Id.downloadDataLinearLayout);
            buttonDownload.SetOnClickListener(this);

            var buttonSettings = FindViewById<LinearLayout>(Resource.Id.settingsLinearLayout);
            buttonSettings.SetOnClickListener(this);

            var buttonPhotos = FindViewById<LinearLayout>(Resource.Id.photoGalleryLinearLayout);
            buttonPhotos.SetOnClickListener(this);

            var buttonAbout = FindViewById<LinearLayout>(Resource.Id.aboutLinearLayout);
            buttonAbout.SetOnClickListener(this);
        }

        private void UpdatePoiCount()
        {
            Task.Run(async () =>
            {
                var poiCount = await AppContextLiveData.Instance.Database.GetItemCount();
                var versionNumber = DependencyService.Get<IAppVersionService>().GetVersionNumber();
                
                var text = String.Format(Resources.GetText(Resource.String.Menu_StatusBarLine1Template), versionNumber, poiCount);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FindViewById<TextView>(Resource.Id.textViewStatusLine).Text = text;
                });
                
            });
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.cameraLinearLayout:
                    Finish();
                    break;
                case Resource.Id.downloadDataLinearLayout:
                    Intent downloadActivityIntent = new Intent(this, typeof(DownloadActivity));
                    StartActivity(downloadActivityIntent);
                    break;
                case Resource.Id.listOfPoisLinearLayout:
                    StartPoisListActivity();
                    break;
                case Resource.Id.settingsLinearLayout:
                    StartSettingsActivity();
                    break;
                case Resource.Id.photoGalleryLinearLayout:
                    Intent photosActivityIntent = new Intent(this, typeof(PhotosActivity));
                    StartActivity(photosActivityIntent);
                    break;
                case Resource.Id.aboutLinearLayout:
                    Intent aboutActivityIntent = new Intent(this, typeof(AboutActivity));
                    StartActivity(aboutActivityIntent);
                    break;
                /*case Resource.Id.paymentLinearLayout:
                    Intent paymentActivityIntent = new Intent(this, typeof(PaymentActivity));
                    StartActivity(paymentActivityIntent);
                    break;*/
            }
        }

        private void StartPoisListActivity()
        {
            Intent listActivityIntent = new Intent(this, typeof(PoiListActivity));
            listActivityIntent.PutExtra("latitude", _location.Latitude);
            listActivityIntent.PutExtra("longitude", _location.Longitude);
            listActivityIntent.PutExtra("altitude", _location.Altitude);
            listActivityIntent.PutExtra("maxDistance", _maxDistance);
            listActivityIntent.PutExtra("minAltitude", _minAltitude);
            StartActivityForResult(listActivityIntent, PoiListActivity.REQUEST_SHOW_POI_LIST);
        }

        private void StartSettingsActivity()
        {
            Intent settingsActivityIntent = new Intent(this, typeof(SettingsActivity));
            StartActivityForResult(settingsActivityIntent,SettingsActivity.REQUEST_SHOW_SETTINGS);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PoiListActivity.REQUEST_SHOW_POI_LIST)
            {
                if (resultCode == (Result)PoiListActivity.RESULT_OK_AND_CLOSE_PARENT)
                {
                    SetResult(Result.Ok);
                    Finish();
                }
            }
            else if (requestCode == SettingsActivity.REQUEST_SHOW_SETTINGS)
            {
                Recreate();
            }

        }
    }
}