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
using Xamarin.Essentials;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Enums;
using AndroidX.CardView.Widget;

namespace Peaks360App.Activities
{
    [Activity(Label = "PoiListActivity")]
    public class PoiListActivity : CardBaseActivity, IPoiActionListener, View.IOnClickListener, SearchView.IOnQueryTextListener
    {
        public static int REQUEST_SHOW_POI_LIST = Definitions.BaseResultCode.POILIST_ACTIVITY;
        public enum ContextType { None = 0, Live = 1, Static = 2}

        public static Result RESULT_CANCELED { get { return Result.Canceled; } }
        public static Result RESULT_OK { get { return Result.Ok; } }
        public static Result RESULT_OK_AND_CLOSE_PARENT { get { return (Result)2; } }


        private ListView _listViewPoi;
        private Spinner _spinnerSelection;
        private Spinner _spinnerCountry;
        private Spinner _spinnerCategory;
        private SearchView _editTextSearch;

        private PoiListItemAdapter _adapter;
        private GpsLocation _location = new GpsLocation();
        private Timer _searchTimer = new Timer();
        private IGpsUtilities _iGpsUtilities = new GpsUtilities();
        private ContextType _contextType;

        private IAppContext Context { 
            get 
            {
                switch (_contextType)
                {
                    case ContextType.None:
                    case ContextType.Live:
                        return AppContextLiveData.Instance;
                    case ContextType.Static:
                        return AppContextStaticData.Instance;
                    default: throw new SystemException("Unsupported context type");
                }
            } 
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            _contextType = (ContextType)Intent.GetShortExtra("contextType", (short)PoiListActivity.ContextType.None);

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

            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                AddCard(Resource.Id.cardSearching, Resource.Id.cardSearchingButton, Resource.Id.cardSearchingLayout, Resource.Id.cardSearchingContent);
            }

            _editTextSearch = FindViewById<SearchView>(Resource.Id.editTextSearch);
            _editTextSearch.Iconified = false;
            _editTextSearch.SetQueryHint(Resources.GetText(Resource.String.Common_Search));
            _editTextSearch.SetOnQueryTextListener(this);
            _editTextSearch.FocusableViewAvailable(_listViewPoi);
            _editTextSearch.ClearFocus();

            _spinnerSelection = FindViewById<Spinner>(Resource.Id.spinnerSelection);

            var filterOptions = GetFilterOptions();
            var selectionAdapter = new ArrayAdapter(this, 
                Android.Resource.Layout.SimpleSpinnerDropDownItem,
                filterOptions.Select(x => x.Description).ToList());
            _spinnerSelection.Adapter = selectionAdapter;
            var filterSelection = _contextType == ContextType.None ? PoiFilter.ByName : Context.SelectedPoiFilter;
            var filterSelectionIdx = filterOptions.FindIndex(x => x.PoiFilter == filterSelection);
            _spinnerSelection.SetSelection(filterSelectionIdx);
            _spinnerSelection.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(OnFilterSelectionChanged);

            _spinnerCountry = FindViewById<Spinner>(Resource.Id.spinnerCountry);
            _spinnerCountry.Adapter = new CountryAdapter(this, true);
            _spinnerCountry.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(OnFilterCountryChanged);

            _spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            _spinnerCategory.Adapter = new CategoryAdapter(this, true);
            _spinnerCategory.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(OnFilterCategoryChanged);
        }

        private List<PoiFilterItem> GetFilterOptions()
        {
            var list = new List<PoiFilterItem>();
            if (_contextType != ContextType.None)
            {
                list.Add(new PoiFilterItem(PoiFilter.VisiblePoints, Resources.GetText(Resource.String.PoiListFilter_Visible)));
            }

            list.Add(new PoiFilterItem(PoiFilter.MyPoints, Resources.GetText(Resource.String.PoiListFilter_My)));
            list.Add(new PoiFilterItem(PoiFilter.ByName, Resources.GetText(Resource.String.PoiListFilter_ByName)));

            return list;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PoiListActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var buttonFavourite = menu.FindItem(Resource.Id.menu_favourite);
            buttonFavourite.SetIcon(Context.ShowFavoritePoisOnly ? Android.Resource.Drawable.ButtonStarBigOn : Android.Resource.Drawable.ButtonStarBigOff);

            var buttonSort = menu.FindItem(Resource.Id.menu_sort);
            buttonSort.SetIcon(Context.PoiSorting == PoiSorting.ByName ? Android.Resource.Drawable.IcMenuSortAlphabetically : Android.Resource.Drawable.IcMenuSortBySize);

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
                case Resource.Id.menu_sortAlphabetically:
                    Context.PoiSorting = PoiSorting.ByName;
                    ShowData();
                    InvalidateOptionsMenu();
                    break;
                case Resource.Id.menu_sortByDistance:
                    Context.PoiSorting = PoiSorting.ByDistance;
                    ShowData();
                    InvalidateOptionsMenu();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void RestartTimer()
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void OnSearchTimerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _searchTimer.Stop();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowData();
            });
        }

        private void OnFilterCountryChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            RestartTimer();
        }

        private void OnFilterCategoryChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            RestartTimer();
        }

        private void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            OnPoiEdit(e.Position);
        }

        private void OnFilterSelectionChanged(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var selection = GetFilterSelection(e.Position);
            if (_contextType != ContextType.None)
            {
                Context.SelectedPoiFilter = selection;
            }

            var advancedSearchVisibility = (selection == PoiFilter.ByName ? ViewStates.Visible : ViewStates.Gone);
            FindViewById<CardView>(Resource.Id.cardSearching).Visibility = advancedSearchVisibility;

            ShowData();
        }

        private PoiFilter GetFilterSelection(int index)
        {
            return GetFilterOptions().ElementAt(index).PoiFilter;
        }

        private IEnumerable<PoiViewItem> GetVisiblePois()
        {
            var items = Context.PoiData;
            return items;
        }

        private IEnumerable<PoiViewItem> GetMyPois()
        {
            var poiList = Context.Database.GetMyItems();
            List<PoiViewItem> items = new PoiViewItemList(poiList, _location, _iGpsUtilities);
            return items;
        }

        private IEnumerable<PoiViewItem> GetByName()
        {
            var country = (_spinnerCountry.Adapter as CountryAdapter)[_spinnerCountry.SelectedItemPosition];

            var category = (_spinnerCategory.Adapter as CategoryAdapter)[_spinnerCategory.SelectedItemPosition];

            string poiName = null;
            if (_editTextSearch.Query.Length > 0)
            {
                poiName = _editTextSearch.Query;
            }

            IEnumerable<Poi> poiList;
            if (!string.IsNullOrEmpty(poiName) || country.HasValue || category.HasValue || Context.ShowFavoritePoisOnly)
            {
                poiList = Context.Database.FindItems(poiName, category, country, Context.ShowFavoritePoisOnly);
            }
            else
            {
                poiList = new List<Poi>();
            }
            List<PoiViewItem> items = new PoiViewItemList(poiList, _location, _iGpsUtilities);
            return items;
        }

        private void ShowData()
        {
            var poiFilterSelection = GetFilterSelection(_spinnerSelection.SelectedItemPosition);
            IEnumerable <PoiViewItem> items;
            switch (poiFilterSelection)
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

            switch (Context.PoiSorting)
            {
                case PoiSorting.ByName:
                    items = items.OrderBy(x => x.Poi.Name);
                    break;
                case PoiSorting.ByDistance:
                    items = items.OrderBy(i => i.GpsLocation.Distance);
                    break;
            }

            _adapter.SetItems(items);
        }

        public void OnPoiDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
            alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
            {
                PoiViewItem item = _adapter[position];
                Context.Database.DeleteItemAsync(item.Poi);
                _adapter.RemoveAt(position);
            });
            alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
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
                    poiViewItem.GpsLocation.Bearing = _iGpsUtilities.Bearing(Context.MyLocation, poiViewItem.GpsLocation);
                    poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(Context.MyLocation, poiViewItem.GpsLocation);
                    poiViewItem.GpsLocation.Distance = _iGpsUtilities.Distance(Context.MyLocation, poiViewItem.GpsLocation);
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

        public void OnClick(View v)
        {
            base.OnClick(v);
        }

        public bool OnQueryTextChange(string newText)
        {
            RestartTimer();
            return true;
        }

        public bool OnQueryTextSubmit(string query)
        {
            _editTextSearch.ClearFocus();
            return true;
        }
    }
}