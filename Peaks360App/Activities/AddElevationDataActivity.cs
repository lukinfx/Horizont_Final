using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Providers;
using Peaks360App.Tasks;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using Xamarin.Essentials;
using GpsUtils = Peaks360Lib.Utilities.GpsUtils;

namespace Peaks360App.Activities
{
    [Activity(Label = "AddElevationDataActivity")]
    public class AddElevationDataActivity : Activity, View.IOnClickListener
    {
        public static int REQUEST_ADD_DATA = Definitions.BaseResultCode.ADDDOWNLOADEDDATA_ACTIVITY + 0;

        private IAppContext AppContext { get { return AppContextLiveData.Instance;} }
        private Poi _selectedPoint;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);
            Platform.Init(this, savedInstanceState);

            if (AppContextLiveData.Instance.IsPortrait)
            {
                SetContentView(Resource.Layout.AddElevationDataActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.AddElevationDataActivityLandscape);
            }

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);
            ActionBar.SetTitle(Resource.String.DownloadElevationDataActivity);

            FindViewById<Button>(Resource.Id.buttonSave).SetOnClickListener(this);
            FindViewById<Button>(Resource.Id.buttonSelect).SetOnClickListener(this);
            FindViewById<LinearLayout>(Resource.Id.linearLayoutSelectedPoint).SetOnClickListener(this);
            FindViewById<RadioButton>(Resource.Id.radioButton100km).SetOnClickListener(this);
            FindViewById<RadioButton>(Resource.Id.radioButton200km).SetOnClickListener(this);
            FindViewById<RadioButton>(Resource.Id.radioButton300km).SetOnClickListener(this);

            if (Peaks360Lib.Utilities.GpsUtils.HasLocation(AppContext.MyLocation))
            {
                _selectedPoint = PoiSelectActivity.GetMyLocationPoi(AppContext);
            }
            else
            {
                _selectedPoint = GetUnknownPoi(AppContext);
            }

            OnSelectionUpdated(_selectedPoint);
            CalculateDownloadSize(_selectedPoint);
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

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.radioButton100km:
                case Resource.Id.radioButton200km:
                case Resource.Id.radioButton300km:
                    CalculateDownloadSize(_selectedPoint);
                    break;
                case Resource.Id.buttonSelect:
                case Resource.Id.linearLayoutSelectedPoint:
                    Intent intent = new Intent(this, typeof(PoiSelectActivity));
                    StartActivityForResult(intent, PoiSelectActivity.REQUEST_SELECT_POI);
                    break;
                case Resource.Id.buttonSave:
                    DownloadElevationData(_selectedPoint);
                    break;
            }
        }

        public Poi GetUnknownPoi(IAppContext context)
        {
            var poi = new Poi()
            {
                Id = (long)PoiId.NO_LOCATION,
                Name = Resources.GetText(Resource.String.DownloadED_PleaseChooseLocation),
                Country = null,
                Longitude = 0,
                Latitude = 0,
                Altitude = 0,
                Category = PoiCategory.Other
            };

            return poi;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PoiSelectActivity.REQUEST_SELECT_POI)
            {
                if (resultCode == PoiSelectActivity.RESULT_OK)
                {
                    var id = data.GetLongExtra("Id", 0);

                    _selectedPoint = (id == (long)PoiId.CURRENT_LOCATION) 
                        ? PoiSelectActivity.GetMyLocationPoi(AppContext)
                        : AppContext.Database.GetItem(id);

                    OnSelectionUpdated(_selectedPoint);
                    CalculateDownloadSize(_selectedPoint);
                }
            }
        }

        private void OnSelectionUpdated(Poi poi)
        {
            FindViewById<TextView>(Resource.Id.textViewPlaceName).Text = poi.Name;
            FindViewById<TextView>(Resource.Id.textViewPlaceCountry).Text = PoiCountryHelper.GetCountryName(poi.Country);
            FindViewById<TextView>(Resource.Id.textViewAltitude).Text = $"{poi.Altitude:F0} m";
            FindViewById<TextView>(Resource.Id.textViewGpsLocation).Text = GpsUtils.LocationAsString(poi.Latitude, poi.Longitude);

            var thumbnail = FindViewById<ImageView>(Resource.Id.Thumbnail);
            thumbnail.SetImageResource(PoiCategoryHelper.GetImage(poi.Category));

            var downloadButton = FindViewById<Button>(Resource.Id.buttonSave);
            downloadButton.Enabled = (poi.Id >= (long)PoiId.FIRST_VALID_ID) || (poi.Id == (long)PoiId.CURRENT_LOCATION);
        }

        private int GetDownloadDistance()
        {
            if (FindViewById<RadioButton>(Resource.Id.radioButton100km).Checked)
            {
                return 100;
            }
            else if (FindViewById<RadioButton>(Resource.Id.radioButton200km).Checked)
            {
                return 200;
            }
            else if (FindViewById<RadioButton>(Resource.Id.radioButton300km).Checked)
            {
                return 300;
            }

            throw new ApplicationException("Distance is not selected");
        }

        private void CalculateDownloadSize(Poi poi)
        {
            long size = 0;
            if (poi != null && poi.Id != (long)PoiId.NO_LOCATION)
            {
                var distance = GetDownloadDistance();

                var gpsLocation = new GpsLocation(poi.Longitude, poi.Latitude, poi.Altitude);
                var etc = new ElevationTileCollection(gpsLocation, distance);

                size = etc.GetSizeToDownload();
            }

            
            FindViewById<TextView>(Resource.Id.textViewDownloadSize).Text =
                String.Format(Resources.GetText(Resource.String.DownloadED_ExpectedSize), size.ToString());
        }

        private void DownloadElevationData(Poi poi)
        {
            if (poi == null)
            {
                return;
            }

            var ded = new DownloadedElevationData()
            {
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                PlaceName = $"{poi.Name}/{PoiCountryHelper.GetCountryName(poi.Country.Value)}",
                Distance = GetDownloadDistance()
            };

            var gpsLocation = new GpsLocation(poi.Longitude, poi.Latitude, poi.Altitude);
            var ed = new ElevationDownload(gpsLocation, ded.Distance);

            if (ed.GetCountToDownload() == 0)
            {
                PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.DownloadED_NoDataToDownload));
                return;
            }


            AppContext.DownloadedElevationDataModel.InsertItem(ded);

            var pd = new ProgressDialog(this);
            pd.SetMessage(Resources.GetText(Resource.String.Download_Progress_DownloadingElevationData));
            pd.SetCancelable(false);
            pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
            pd.Max = 100;
            pd.Show();

            ed.OnFinishedAction = (result) =>
            {
                pd.Hide();
                if (!string.IsNullOrEmpty(result))
                {
                    PopupHelper.ErrorDialog(this, result);
                }
                else
                {
                    ded.SizeInBytes = ed.GetSize();
                    AppContext.DownloadedElevationDataModel.UpdateItem(ded);
                    Finish();
                }
            };
            ed.OnProgressChange = (progress) =>
            {
                MainThread.BeginInvokeOnMainThread(() => { pd.Progress = progress; });
                Thread.Sleep(50);
            };

            ed.Execute();
        }
    }
}