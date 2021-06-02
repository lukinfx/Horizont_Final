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
        public static int REQUEST_SELECT_DOWNLOADELEVATIONDATAAREA = Definitions.BaseResultCode.POISELECT_ACTIVITY + 0;
        public static int REQUEST_SELECT_CAMERALOCATION = Definitions.BaseResultCode.POISELECT_ACTIVITY + 1;
        public static int REQUEST_SELECT_CAMERADIRECTION = Definitions.BaseResultCode.POISELECT_ACTIVITY + 2;

        public static Result RESULT_CANCELED { get { return Result.Canceled; } }
        public static Result RESULT_OK { get { return Result.Ok; } }

        private IAppContext AppContext { get { return AppContextLiveData.Instance; } }
        private ListView _listViewPoi;
        private SearchView _searchViewPlaceName;
        private PoiListItemAdapter _adapter;
        private Timer _changeFilterTimer = new Timer();
        private IGpsUtilities _iGpsUtilities = new GpsUtilities();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.PoiSelectActivity);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true); 
            ActionBar.SetTitle(Resource.String.PoiSelectActivity);

            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;


            _searchViewPlaceName = FindViewById<SearchView>(Resource.Id.searchViewPlaceName);
            _searchViewPlaceName.Iconified = false;
            _searchViewPlaceName.SetQueryHint(Resources.GetText(Resource.String.Common_Search));
            _searchViewPlaceName.SetOnQueryTextListener(this);
            _searchViewPlaceName.FocusableViewAvailable(_listViewPoi);

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

        public void FilterPlaces(string filterText)
        {
            Task.Run(async () =>
            {
                var poiItems = await AppContext.Database.FindItemsAsync(filterText);
                var poiViewItems = new PoiViewItemList(poiItems.OrderBy(x => x.Name), AppContext.MyLocation, _iGpsUtilities);

                /*if (!poiViewItems.Any() && Peaks360Lib.Utilities.GpsUtils.HasLocation(AppContext.MyLocation))
                {
                    AddMyLocation(poiViewItems);
                }*/

                MainThread.BeginInvokeOnMainThread(() => _adapter.SetItems(poiViewItems));
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
            FilterPlaces(_searchViewPlaceName.Query);
        }
    }
}