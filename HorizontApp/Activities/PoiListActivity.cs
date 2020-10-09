using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Timers;

using Xamarin.Essentials;
using static Android.Views.View;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

using HorizontApp.Activities;
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;

namespace HorizontApp.Views.ListOfPoiView
{
    [Activity(Label = "PoiListActivity")]
    public class PoiListActivity : Activity, IPoiActionListener
    {
        private static int ReqCode_AddPoiActivity = 1;

        private ListView _listViewPoi;
        private Spinner _spinnerSelection;
        private EditText _editTextSearch;

        private ListViewAdapter _adapter;
        private GpsLocation _location = new GpsLocation();
        private List<PoiViewItem> _items;
        private Timer _searchTimer = new Timer();
        private PoiDatabase _database;
        private String[] _listOfSelections = new String[] { "Visible points", "My points", "Find by name"};
        private double _maxDistance; 
        private double _minAltitude;

        public PoiDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new PoiDatabase();
                }
                return _database;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _location.Latitude = Intent.GetDoubleExtra("latitude", 0);
            _location.Longitude = Intent.GetDoubleExtra("longitude", 0);
            _location.Altitude = Intent.GetDoubleExtra("altitude", 0);
            
            //TODO: we can get minAltitude and maxDistance - not needed as activity parameter
            _maxDistance = Intent.GetIntExtra("maxDistance", 0);
            _minAltitude = Intent.GetIntExtra("minAltitude", 0);

            SetContentView(Resource.Layout.PoiListActivity);
            // Create your application here

            

            
            InitializeUI();
            _selectByDistance();
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

            var poiList = Database.GetItems(_location, _maxDistance);

            _items = new PoiViewItemList(poiList, _location, _maxDistance, _minAltitude, false);
            _items = _items.OrderBy(i => i.Distance).ToList();
            _adapter = new ListViewAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;

            _listViewPoi.ItemClick += OnListItemClick;

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(false);


            _editTextSearch = FindViewById<EditText>(Resource.Id.editTextSearch);
            _editTextSearch.TextChanged += editTextSearch_TextChanged;

            _spinnerSelection = FindViewById<Spinner>(Resource.Id.spinnerSelection);
            var selectionAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfSelections.ToList());
            _spinnerSelection.Adapter = selectionAdapter;
            _spinnerSelection.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(Selection_ItemSelected);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PoiListActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void OnSearchTimerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _searchTimer.Stop();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _FindItem();
            });
        }

        private void _FindItem()
        {
            var poiList = Database.FindItems(_editTextSearch.Text);

            _items = new PoiViewItemList(poiList, _location);
            _items = _items.OrderBy(i => i.Distance).ToList();
            _adapter = new ListViewAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;
            _listViewPoi.Invalidate();
        }

        private void editTextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            OnPoiEdit(e.Position);
        }

        private void _selectByDistance()
        {
            //TODO: get minAltitude and maxDistance from CompassViewSettings
            var poiList = Database.GetItems(_location, _maxDistance);

            _items = new PoiViewItemList(poiList, _location, _maxDistance, _minAltitude, false);
            _items = _items.OrderBy(i => i.Distance).ToList();
            _adapter = new ListViewAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;
            _listViewPoi.Invalidate();
        }

        private void _selectMyPois()
        {
            var poiList = Database.GetMyItems();

            _items = new PoiViewItemList(poiList, _location);
            _items = _items.OrderBy(i => i.Distance).ToList();
            _adapter = new ListViewAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;
            _listViewPoi.Invalidate();
        }

        private void Selection_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            int selection = e.Position;
            if (selection == 0)
            {
                _editTextSearch.Visibility = ViewStates.Invisible;
                _selectByDistance();
            } else if (selection == 1)
            {
                _editTextSearch.Visibility = ViewStates.Invisible;
                _selectMyPois();
            }
            else if (selection == 2)
            {
                _editTextSearch.Visibility = ViewStates.Visible;
            }
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
            }
            return base.OnOptionsItemSelected(item);
        }

        public void OnPoiDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                PoiViewItem item = _items[position];
                _database.DeleteItemAsync(item.Poi);
                _items.Remove(item);
                _adapter = new ListViewAdapter(this, _items, this);
                _listViewPoi.Adapter = _adapter;
            });
            alert.SetNegativeButton("No", (senderAlert, args) => { });
            alert.SetMessage("Are you sure you want to delete this item?");
            var answer = alert.Show();


            _adapter.NotifyDataSetChanged();
        }

        public void OnPoiEdit(int position)
        {
            PoiViewItem item = _items[position];
            Intent editActivityIntent = new Intent(this, typeof(EditActivity));
            editActivityIntent.PutExtra("Id", item.Poi.Id);
            StartActivity(editActivityIntent);

            _adapter = new ListViewAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;
        }

        public void OnPoiAdd()
        {
            Intent editActivityIntent = new Intent(this, typeof(EditActivity));
            StartActivityForResult(editActivityIntent, ReqCode_AddPoiActivity);

            _adapter = new ListViewAdapter(this, _items, this);
            _listViewPoi.Adapter = _adapter;
        }


        public void OnPoiLike(int position)
        {
            PoiViewItem item = _items[position];
            item.Poi.Favorite = !item.Poi.Favorite;
            _adapter.NotifyDataSetChanged();
            Database.UpdateItemAsync(item.Poi);    
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == ReqCode_AddPoiActivity)
            {
                //ReloadData(favourite);
            }
        }
    }
}