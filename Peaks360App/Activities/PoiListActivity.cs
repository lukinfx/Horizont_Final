using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.ViewModel;
using Xamarin.Essentials;
using Peaks360App.Activities;
using Peaks360App.AppContext;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360App.Utilities;
using Peaks360Lib.Utilities;

namespace Peaks360App.Views.ListOfPoiView
{
    [Activity(Label = "PoiListActivity")]
    public class PoiListActivity : Activity, IPoiActionListener
    {
        public static int REQUEST_SHOW_POI_LIST = Definitions.BaseResultCode.POILIST_ACTIVITY;

        public static Result RESULT_CANCELED { get { return Result.Canceled; } }
        public static Result RESULT_OK { get { return Result.Ok; } }
        public static Result RESULT_OK_AND_CLOSE_PARENT { get { return (Result)2; } }


        private ListView _listViewPoi;
        private Spinner _spinnerSelection;
        private EditText _editTextSearch;

        private PoiListItemAdapter _adapter;
        private GpsLocation _location = new GpsLocation();
        private Timer _searchTimer = new Timer();

        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            _location.Latitude = Intent.GetDoubleExtra("latitude", 0);
            _location.Longitude = Intent.GetDoubleExtra("longitude", 0);
            _location.Altitude = Intent.GetDoubleExtra("altitude", 0);
            
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.PoiListActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.PoiListActivityLandscape);
            }

            InitializeUI();
            ShowData();

            InitializeSearchTimer();
        }

        private void InitializeSearchTimer()
        {
            _searchTimer.Interval = 500;
            _searchTimer.AutoReset = false;
            _searchTimer.Elapsed += OnSearchTimerTimerElapsed;
        }

        private void InitializeUI()
        {
            _listViewPoi = FindViewById<ListView>(Resource.Id.listViewPoi);

            List<PoiViewItem> _items = Context.PoiData;
            _items = _items.OrderBy(i => i.GpsLocation.Distance).ToList();
            _adapter = new PoiListItemAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;

            _listViewPoi.ItemClick += OnListItemClick;

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(false);


            _editTextSearch = FindViewById<EditText>(Resource.Id.editTextSearch);
            _editTextSearch.TextChanged += OnSearchTextChanged;

            _spinnerSelection = FindViewById<Spinner>(Resource.Id.spinnerSelection);
            

            var selectionAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, GetFilterOptions());
            _spinnerSelection.Adapter = selectionAdapter;
            _spinnerSelection.SetSelection((int)Context.SelectedPoiFilter);
            _spinnerSelection.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(OnFilterSelectionChanged);
        }

        private List<string> GetFilterOptions()
        {
            var list = new string[]
            {
                Resources.GetText(Resource.String.PoiListFilter_Visible),
                Resources.GetText(Resource.String.PoiListFilter_My),
                Resources.GetText(Resource.String.PoiListFilter_ByName),
            };
            return list.ToList();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PoiListActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var buttonFavourite = menu.GetItem(1);
            buttonFavourite.SetIcon(Context.ShowFavoritePoisOnly ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
                case Resource.Id.menu_addNew:
                    OnPoiAdd();
                    break;
                case Resource.Id.menu_favourite:
                    Context.ToggleFavouritePois();
                    ShowData();
                    InvalidateOptionsMenu();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void OnSearchTimerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _searchTimer.Stop();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowData();
            });
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            OnPoiEdit(e.Position);
        }

        private void OnFilterSelectionChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            int selection = e.Position;
            if (selection == 0)
            {
                Context.SelectedPoiFilter = PoiFilter.VisiblePoints;
                _editTextSearch.Visibility = ViewStates.Invisible;

            }
            else if (selection == 1)
            {
                Context.SelectedPoiFilter = PoiFilter.MyPoints;
                _editTextSearch.Visibility = ViewStates.Invisible;
            }
            else if (selection == 2)
            {
                Context.SelectedPoiFilter = PoiFilter.ByName;
                _editTextSearch.Visibility = ViewStates.Visible;
            }

            ShowData();
        }

        private IEnumerable<PoiViewItem> GetVisiblePois()
        {
            var items = Context.PoiData;
            return items;
        }

        private IEnumerable<PoiViewItem> GetMyPois()
        {
            var poiList = Context.Database.GetMyItems();
            List<PoiViewItem> items = new PoiViewItemList(poiList, _location);
            return items;
        }

        private IEnumerable<PoiViewItem> GetByName()
        {
            IEnumerable<Poi> poiList;
            if (_editTextSearch.Text.Length > 0)
            {
                poiList = Context.Database.FindItems(_editTextSearch.Text);
            }
            else
            {
                poiList = new List<Poi>();
            }
            List<PoiViewItem> items = new PoiViewItemList(poiList, _location);
            return items;
        }

        private void ShowData()
        {
            IEnumerable<PoiViewItem> items;
            switch (Context.SelectedPoiFilter)
            {
                case PoiFilter.VisiblePoints:
                    items = GetVisiblePois();
                    break;
                case PoiFilter.MyPoints:
                    items = GetMyPois();
                    break;
                case PoiFilter.ByName:
                    items = GetByName();
                    break;
                default:
                    return;
            }

            if (Context.ShowFavoritePoisOnly)
            {
                items = items.Where(x => x.Poi.Favorite);
            }

            items = items.OrderBy(i => i.GpsLocation.Distance).ToList();
            _adapter = new PoiListItemAdapter(this, items, this);
            _listViewPoi.Adapter = _adapter;
            _listViewPoi.Invalidate();
        }

        public void OnPoiDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) =>
            {
                PoiViewItem item = _adapter[position];
                Context.Database.DeleteItemAsync(item.Poi);
                _adapter.RemoveAt(position);
            });
            alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) => { });
            alert.SetMessage(Resources.GetText(Resource.String.PoiListDeleteQuestion));
            var answer = alert.Show();


            _adapter.NotifyDataSetChanged();
        }

        public void OnPoiEdit(int position)
        {
            PoiViewItem item = _adapter[position];
            Intent editActivityIntent = new Intent(this, typeof(EditActivity));
            editActivityIntent.PutExtra("Id", item.Poi.Id);
            StartActivityForResult(editActivityIntent, EditActivity.REQUEST_EDIT_POI);
        }

        public void OnPoiAdd()
        {
            Intent editActivityIntent = new Intent(this, typeof(EditActivity));
            StartActivityForResult(editActivityIntent, EditActivity.REQUEST_ADD_POI);
        }

        public void OnPoiLike(int position)
        {
            PoiViewItem item = _adapter[position];
            item.Poi.Favorite = !item.Poi.Favorite;
            _adapter.NotifyDataSetChanged();
            Context.Database.UpdateItemAsync(item.Poi);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == EditActivity.REQUEST_ADD_POI)
            {
                if (resultCode == EditActivity.RESULT_OK)
                {
                    var id = data.GetLongExtra("Id", 0);
                    var item = Context.Database.GetItem(id);

                    var poiViewItem = new PoiViewItem(item);
                    poiViewItem.GpsLocation.Bearing = Utilities.GpsUtils.QuickBearing(Context.MyLocation, poiViewItem.GpsLocation);
                    poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(Context.MyLocation, poiViewItem.GpsLocation);
                    poiViewItem.GpsLocation.Distance = Utilities.GpsUtils.QuickDistance(Context.MyLocation, poiViewItem.GpsLocation);
                    _adapter.Add(poiViewItem);
                }
                if (resultCode == EditActivity.RESULT_OK_AND_CLOSE_PARENT)
                {
                    SetResult(RESULT_OK_AND_CLOSE_PARENT);
                    Finish();
                }
            }

            if (requestCode == EditActivity.REQUEST_EDIT_POI)
            {
                if (resultCode == EditActivity.RESULT_OK)
                {
                    var id = data.GetLongExtra("Id", 0);
                    var itemfromDb = Context.Database.GetItem(id);
                    var itemfromAdapter = _adapter.GetPoiItem(id);
                    itemfromAdapter.Poi = itemfromDb;

                    _adapter.NotifyDataSetChanged();
                }
                if (resultCode == EditActivity.RESULT_OK_AND_CLOSE_PARENT)
                {
                    SetResult(RESULT_OK_AND_CLOSE_PARENT);
                    Finish();
                }
            }
        }
    }
}