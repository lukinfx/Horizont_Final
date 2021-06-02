using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using Xamarin.Essentials;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/PoiSelectActivity")]
    public class PoiSelectActivity : Activity, IPoiActionListener, SearchView.IOnQueryTextListener
    {
        public enum SortBy { Name, Distance }

        public static int REQUEST_SELECT_DOWNLOADELEVATIONDATAAREA = Definitions.BaseResultCode.POISELECT_ACTIVITY + 0;
        public static int REQUEST_SELECT_CAMERALOCATION = Definitions.BaseResultCode.POISELECT_ACTIVITY + 1;
        public static int REQUEST_SELECT_CAMERADIRECTION = Definitions.BaseResultCode.POISELECT_ACTIVITY + 2;

        public static Result RESULT_CANCELED { get { return Result.Canceled; } }
        public static Result RESULT_OK { get { return Result.Ok; } }

        private IAppContext AppContext { get { return AppContextLiveData.Instance; } }
        private ListView _listViewPoi;
        private SearchView _searchViewText;
        private Spinner _spinnerCountry;
        private Spinner _spinnerCategory;
        private PoiListItemAdapter _adapter;
        private Timer _changeFilterTimer = new Timer();
        private IGpsUtilities _iGpsUtilities = new GpsUtilities();
        private GpsLocation _centerGpsLocation;
        private SortBy _sortBy;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.PoiSelectActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.PoiSelectActivityLandscape);
            }
            

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true); 
            ActionBar.SetTitle(Resource.String.PoiSelectActivity);

            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;

            var longitude = Intent.GetDoubleExtra("Longitude", Double.NaN);
            var latitude = Intent.GetDoubleExtra("Latitude", Double.NaN);
            var sortBy = Intent.GetStringExtra("SortBy");

            if (longitude != Double.NaN && latitude != Double.NaN)
            {
                _centerGpsLocation = new GpsLocation(longitude, latitude, 0);
            }

            if (!Enum.TryParse(sortBy, out _sortBy))
            {
                _sortBy = SortBy.Name;
            }

            _searchViewText = FindViewById<SearchView>(Resource.Id.editTextSearch);
            _searchViewText.Iconified = false;
            _searchViewText.SetQueryHint(Resources.GetText(Resource.String.Common_Search));
            _searchViewText.SetOnQueryTextListener(this);
            _searchViewText.FocusableViewAvailable(_listViewPoi);

            _spinnerCountry = FindViewById<Spinner>(Resource.Id.spinnerCountry);
            _spinnerCountry.Adapter = new CountryAdapter(this, true);
            _spinnerCountry.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(OnFilterCountryChanged);

            _spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            _spinnerCategory.Adapter = new CategoryAdapter(this, true);
            _spinnerCategory.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(OnFilterCategoryChanged);

            _listViewPoi = FindViewById<ListView>(Resource.Id.listViewPoi);

            var poiViewItems = new PoiViewItemList(null, AppContext.MyLocation, _iGpsUtilities);
            
            if (Intent.Action == REQUEST_SELECT_DOWNLOADELEVATIONDATAAREA.ToString())
            {
                if (Peaks360Lib.Utilities.GpsUtils.HasLocation(AppContext.MyLocation))
                {
                    AddMyLocation(poiViewItems);
                }
            }

            _adapter = new PoiListItemAdapter(this, poiViewItems, this, false);
            _listViewPoi.Adapter = _adapter;
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

        public void OnPoiDelete(int position) { }

        public void OnPoiLike(int position) { }

        public void OnPoiEdit(int position)
        {
            var poi = _adapter[position];

            var resultIntent = new Intent();
            resultIntent.PutExtra("Id", poi.Poi.Id);
            SetResult(RESULT_OK, resultIntent);

            Finish();
        }

        public void FilterPlaces(string name, PoiCountry? country, PoiCategory? category)
        {
            Task.Run(() =>
            {
                IEnumerable<Poi> poiList;
                if (!string.IsNullOrEmpty(name) || country.HasValue || category.HasValue)
                {
                    poiList = AppContext.Database.FindItems(name, category, country, false);
                }
                else
                {
                    poiList = new List<Poi>();
                }
                
                IEnumerable<PoiViewItem> items = new PoiViewItemList(poiList, _centerGpsLocation ?? AppContext.MyLocation, _iGpsUtilities);
                items = (_sortBy == SortBy.Name) ? items.OrderBy(x => x.Poi.Name) : items.OrderBy(x => x.GpsLocation.Distance);
                MainThread.BeginInvokeOnMainThread(() => _adapter.SetItems(items));
            });
        }

        private void AddMyLocation(PoiViewItemList poiViewItems)
        {
            var myLocation = new PoiViewItem(GetMyLocationPoi(AppContext));
            myLocation.GpsLocation.Distance = 0;
            myLocation.GpsLocation.Bearing = 0;
            myLocation.GpsLocation.VerticalViewAngle = 0;

            poiViewItems.Insert(0, myLocation);
        }

        public static Poi GetMyLocationPoi(IAppContext context)
        {
            var poi = new Poi()
            {
                Id = (long)PoiId.CURRENT_LOCATION,
                Name = context.MyLocationPlaceInfo.PlaceName,
                Country = context.MyLocationPlaceInfo.Country,
                Longitude = context.MyLocation.Longitude,
                Latitude = context.MyLocation.Latitude,
                Altitude = context.MyLocation.Altitude,
                Category = PoiCategory.Other
            };

            return poi;
        }

        private void OnFilterCountryChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            RestartTimer();
        }

        private void OnFilterCategoryChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            RestartTimer();
        }

        public bool OnQueryTextChange(string newText)
        {
            RestartTimer();
            return true;
        }

        public bool OnQueryTextSubmit(string query)
        {
            return true;
        }

        private void RestartTimer()
        {
            _changeFilterTimer.Stop();
            _changeFilterTimer.Start();
        }

        private void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _changeFilterTimer.Stop();
            
            var country = (_spinnerCountry.Adapter as CountryAdapter)[_spinnerCountry.SelectedItemPosition];

            var category = (_spinnerCategory.Adapter as CategoryAdapter)[_spinnerCategory.SelectedItemPosition];
            var name = _searchViewText.Query;

            FilterPlaces(name, country, category);
        }
    }
}