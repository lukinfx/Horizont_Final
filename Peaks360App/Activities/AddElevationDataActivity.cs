using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
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

namespace Peaks360App.Activities
{
    [Activity(Label = "AddElevationDataActivity")]
    public class AddElevationDataActivity : Activity, IPoiActionListener, SearchView.IOnQueryTextListener, View.IOnClickListener
    {
        public static int REQUEST_ADD_DATA = Definitions.BaseResultCode.ADDDOWNLOADEDDATA_ACTIVITY + 0;

        private IAppContext AppContext { get { return AppContextLiveData.Instance;} }
        private ListView _listViewPoi;
        private SearchView _searchViewPlaceName;
        private PoiListItemAdapter _adapter;
        private int? _selectedItem;

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

            /*var _spinnerCountry = FindViewById<Spinner>(Resource.Id.spinnerCountry);
            List<PoiCountry> _poiCountries = PoiCountryHelper.GetAllCountries().OrderBy(x => PoiCountryHelper.GetCountryName(x)).ToList();
            var countryList = _poiCountries.Select(x => PoiCountryHelper.GetCountryName(x)).ToList();
            _spinnerCountry.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, countryList);
            */

            _searchViewPlaceName = FindViewById<SearchView>(Resource.Id.searchViewPlaceName);
            _searchViewPlaceName.Iconified = false;
            _searchViewPlaceName.SetQueryHint(Resources.GetText(Resource.String.Common_Search));
            _searchViewPlaceName.SetOnQueryTextListener(this);
            _searchViewPlaceName.FocusableViewAvailable(_listViewPoi);

            _listViewPoi = FindViewById<ListView>(Resource.Id.listViewPoi);

            var poiViewItems = new PoiViewItemList(null, AppContext.MyLocation);
            if (Peaks360Lib.Utilities.GpsUtils.HasLocation(AppContext.MyLocation))
            {
                AddMyLocation(poiViewItems);
            }

            _adapter = new PoiListItemAdapter(this, poiViewItems, this, false);
            _listViewPoi.Adapter = _adapter;

            FindViewById<Button>(Resource.Id.buttonSave).SetOnClickListener(this);
            FindViewById<RadioButton>(Resource.Id.radioButton100km).SetOnClickListener(this);
            FindViewById<RadioButton>(Resource.Id.radioButton200km).SetOnClickListener(this);
            FindViewById<RadioButton>(Resource.Id.radioButton300km).SetOnClickListener(this);

            CalculateDownloadSize();
        }

        public void OnPoiDelete(int position) { }
        public void OnPoiLike(int position) { }

        public void OnPoiEdit(int position)
        {
            if (_selectedItem.HasValue)
            {
                _adapter[_selectedItem.Value].Selected = false;
            }

            _adapter[position].Selected = true;
            _selectedItem = position;
            _listViewPoi.SetSelection(position);
            CalculateDownloadSize();

            _adapter.NotifyDataSetChanged();

            _searchViewPlaceName.ClearFocus();
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

        public void FilterPlaces(string filterText)
        {
            Task.Run(async () =>
            {
                var poiItems = await AppContext.Database.FindItemsAsync(filterText);
                var poiViewItems = new PoiViewItemList(poiItems, AppContext.MyLocation);

                if (!poiViewItems.Any() && Peaks360Lib.Utilities.GpsUtils.HasLocation(AppContext.MyLocation))
                {
                    AddMyLocation(poiViewItems);
                }

                MainThread.BeginInvokeOnMainThread(() => _adapter.SetItems(poiViewItems));
            });
        }

        private void AddMyLocation(PoiViewItemList poiViewItems)
        {
            var poi = new Poi()
            {
                Name = AppContext.MyLocationPlaceInfo.PlaceName,
                Country = AppContext.MyLocationPlaceInfo.Country,
                Longitude = AppContext.MyLocation.Longitude,
                Latitude = AppContext.MyLocation.Latitude,
                Altitude = AppContext.MyLocation.Altitude,
                Category = PoiCategory.Other
            };

            var myLocation = new PoiViewItem(poi);
            myLocation.GpsLocation.Distance = 0;
            myLocation.GpsLocation.Bearing = 0;
            myLocation.GpsLocation.VerticalViewAngle = 0;

            poiViewItems.Insert(0, myLocation);
        }

        public bool OnQueryTextChange(string newText)
        {
            FilterPlaces(_searchViewPlaceName.Query);
            return true;
        }

        public bool OnQueryTextSubmit(string query)
        {
            return true;
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.radioButton100km:
                case Resource.Id.radioButton200km:
                case Resource.Id.radioButton300km:
                    CalculateDownloadSize();
                    break;
                case Resource.Id.buttonSave:
                    DownloadElevationData();
                    break;
            }
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

        private void CalculateDownloadSize()
        {
            long size = 0;
            if (_selectedItem.HasValue)
            {
                var distance = GetDownloadDistance();
                var poi = _adapter[_selectedItem.Value];

                var etc = new ElevationTileCollection(poi.GpsLocation, distance);

                size = etc.GetSizeToDownload();
            }

            
            FindViewById<TextView>(Resource.Id.textViewDownloadSize).Text =
                String.Format(Resources.GetText(Resource.String.DownloadED_ExpectedSize), size.ToString());
        }

        private void DownloadElevationData()
        {
            if (!_selectedItem.HasValue)
            {
                return;
            }

            var poi = _adapter[_selectedItem.Value];
            var ded = new DownloadedElevationData()
            {
                Latitude = poi.GpsLocation.Latitude,
                Longitude = poi.GpsLocation.Longitude,
                PlaceName = $"{poi.Poi.Name}/{PoiCountryHelper.GetCountryName(poi.Poi.Country.Value)}",
                Distance = GetDownloadDistance()
            };

            var ed = new ElevationDownload(poi.GpsLocation, ded.Distance);

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