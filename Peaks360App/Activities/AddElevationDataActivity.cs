using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Tasks;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using Xamarin.Essentials;
using GpsUtils = Peaks360Lib.Utilities.GpsUtils;

namespace Peaks360App.Activities
{
    [Activity(Label = "AddElevationDataActivity")]
    public class AddElevationDataActivity : Activity, View.IOnClickListener
    {
        public static int REQUEST_ADD_DATA = Definitions.BaseResultCode.ADDDOWNLOADEDDATA_ACTIVITY + 0;
        public static int REQUEST_EDIT_DATA = Definitions.BaseResultCode.ADDDOWNLOADEDDATA_ACTIVITY + 1;

        private IAppContext AppContext { get { return AppContextLiveData.Instance;} }
        private Poi _selectedPoint;
        private DownloadedElevationData _oldDedItem;
        private IEnumerable<DownloadedElevationData> _allElevationData;

        protected override void OnCreate(Bundle savedInstanceState)
        {
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

            var id = Intent.GetLongExtra("Id", -1);
            if (id != -1)
            {
                _oldDedItem = AppContext.Database.GetDownloadedElevationDataItem(id);
                _selectedPoint = new Poi()
                {
                    Latitude = _oldDedItem.Latitude,
                    Longitude = _oldDedItem.Longitude,
                    Altitude = _oldDedItem.Altitude,
                    Name = _oldDedItem.PlaceName,
                    Country = _oldDedItem.Country
                };
                SetDownloadDistance(_oldDedItem.Distance);
                FindViewById<Button>(Resource.Id.buttonSelect).Enabled = false;
            }
            else if (Peaks360Lib.Utilities.GpsUtils.HasLocation(AppContext.MyLocation))
            {
                _selectedPoint = PoiSelectActivity.GetMyLocationPoi(AppContext);
                FindViewById<Button>(Resource.Id.buttonSelect).Enabled = true;
            }
            else
            {
                _selectedPoint = GetUnknownPoi(AppContext);
                FindViewById<Button>(Resource.Id.buttonSelect).Enabled = true;
            }

            OnSelectionUpdated(_selectedPoint);
            CalculateDownloadSize(_selectedPoint);

            Task.Run(async () => { _allElevationData = await AppContext.Database.GetDownloadedElevationDataAsync(); });
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
                    OnSelectLocationClicked();
                    break;
                case Resource.Id.buttonSave:
                    SaveElevationData(_selectedPoint);
                    break;
            }
        }

        private void OnSelectLocationClicked()
        {
            if (_oldDedItem == null)
            {
                Intent intent = new Intent(this, typeof(PoiSelectActivity));
                intent.SetAction(PoiSelectActivity.REQUEST_SELECT_DOWNLOADELEVATIONDATAAREA.ToString());
                intent.PutExtra("Longitude", AppContext.MyLocation.Longitude);
                intent.PutExtra("Latitude", AppContext.MyLocation.Latitude);
                intent.PutExtra("SortBy", PoiSelectActivity.SortBy.Name.ToString());
                StartActivityForResult(intent, PoiSelectActivity.REQUEST_SELECT_DOWNLOADELEVATIONDATAAREA);
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
            if (requestCode == PoiSelectActivity.REQUEST_SELECT_DOWNLOADELEVATIONDATAAREA)
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

            CalculateDownloadSize(poi);
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

        private void SetDownloadDistance(int distance)
        {
            if (distance == 100)
            {
                FindViewById<RadioButton>(Resource.Id.radioButton100km).Checked = true;
            }
            else if (distance == 200)
            {
                FindViewById<RadioButton>(Resource.Id.radioButton200km).Checked = true;
            }
            else if (distance == 300)
            {
                FindViewById<RadioButton>(Resource.Id.radioButton300km).Checked = true;
            }
            else //default option (it should never happen)
            {
                FindViewById<RadioButton>(Resource.Id.radioButton100km).Checked = true;
            }
        }

        private void CalculateDownloadSize(Poi poi)
        {
            string text = "";
            bool enableDownloadButton = false;

            if (poi?.Id >= (long) PoiId.FIRST_VALID_ID || poi?.Id == (long) PoiId.CURRENT_LOCATION)
            {
                float size;
                var newDistance = GetDownloadDistance();
                if (_oldDedItem == null || newDistance > _oldDedItem.Distance)
                {
                    var gpsLocation = new GpsLocation(poi.Longitude, poi.Latitude, poi.Altitude);
                    var etc = new ElevationTileCollection(gpsLocation, newDistance);

                    size = etc.GetSizeToDownload();
                    text = String.Format(Resources.GetText(Resource.String.DownloadED_ExpectedSizeDownload), $"{size:F1}");
                    enableDownloadButton = true;
                }
                else if (newDistance < _oldDedItem.Distance)
                {
                    var location = new GpsLocation(_oldDedItem.Longitude, _oldDedItem.Latitude, 0);
                    var tilesToBeRemovedAll = ElevationTileCollection.GetTilesForRemoval(location, _oldDedItem.Distance, newDistance);
                    var tilesToBeRemovedUnique = ElevationTileCollection.GetUniqueTilesForRemoval(_oldDedItem.Id, _allElevationData, tilesToBeRemovedAll);
                    
                    size = tilesToBeRemovedUnique.GetSize() / 1024f / 1024f;
                    text = String.Format(Resources.GetText(Resource.String.DownloadED_ExpectedSizeRemove), $"{size:F1}");
                    enableDownloadButton = true;
                }
            }

            FindViewById<TextView>(Resource.Id.textViewDownloadSize).Text = text;
            FindViewById<Button>(Resource.Id.buttonSave).Enabled = enableDownloadButton;
        }

        private void SaveElevationData(Poi poi)
        {
            if (poi == null)
            {
                return;
            }

            var ded = new DownloadedElevationData()
            {
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                Altitude = poi.Altitude,
                PlaceName = poi.Name,
                Country = poi.Country,
                Distance = GetDownloadDistance()
            };

            var gpsLocation = new GpsLocation(poi.Longitude, poi.Latitude, poi.Altitude);
            var ed = new ElevationDataDownload(gpsLocation, ded.Distance);

            /*if (ed.GetCountToDownload() == 0)
            {
                PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.DownloadED_NoDataToDownload));
                return;
            }*/

            if (_oldDedItem != null)
            {
                ded.Id = _oldDedItem.Id;
                if (ded.Distance < _oldDedItem.Distance)
                {
                    RemoveElevationData(ded, _oldDedItem.Distance);
                }
                else
                {
                    DownloadElevationData(ded);
                }
            }
            else
            {
                AppContext.DownloadedElevationDataModel.InsertItem(ded);
                DownloadElevationData(ded);
            }
        }

        private void DownloadElevationData(DownloadedElevationData ded)
        {
            var edd = new ElevationDataDownload(new GpsLocation(ded.Longitude, ded.Latitude, 0), ded.Distance);

            var pd = new ProgressDialog(this);
            pd.SetMessage(Resources.GetText(Resource.String.Download_Progress_DownloadingElevationData));
            pd.SetCancelable(false);
            pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
            pd.Max = 100;
            pd.Show();

            edd.OnFinishedAction = (result) =>
            {
                pd.Hide();
                if (!string.IsNullOrEmpty(result))
                {
                    PopupHelper.ErrorDialog(this, result);
                }
                else
                {
                    ded.SizeInBytes = edd.GetSize();
                    AppContext.DownloadedElevationDataModel.UpdateItem(ded);
                    Finish();
                }
            };
            edd.OnProgressChange = (progress) =>
            {
                MainThread.BeginInvokeOnMainThread(() => { pd.Progress = progress; });
                Thread.Sleep(50);
            };

            edd.Execute();
        }

        private void RemoveElevationData(DownloadedElevationData ded, int oldDedDistance)
        {
            var location = new GpsLocation(ded.Longitude, ded.Latitude, 0);
            var tilesToBeRemovedAll = ElevationTileCollection.GetTilesForRemoval(location, oldDedDistance, ded.Distance);
            var tilesToBeRemovedUnique = ElevationTileCollection.GetUniqueTilesForRemoval(ded.Id, _allElevationData, tilesToBeRemovedAll);
            tilesToBeRemovedUnique.Remove();

            var edd = new ElevationDataDownload(new GpsLocation(ded.Longitude, ded.Latitude, 0), ded.Distance);
            ded.SizeInBytes = edd.GetSize();
            AppContext.DownloadedElevationDataModel.UpdateItem(ded);
            Finish();
        }
    }
}