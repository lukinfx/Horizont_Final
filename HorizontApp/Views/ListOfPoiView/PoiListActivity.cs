using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using HorizontApp.Activities;
using HorizontApp.DataAccess;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;

using HorizontApp.Utilities;
using Xamarin.Essentials;
using static Android.Views.View;

namespace HorizontApp.Views.ListOfPoiView
{
    [Activity(Label = "PoiListActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class PoiListActivity : Activity, IOnClickListener, IPoiActionListener
    {
        private static int ReqCode_AddPoiActivity = 1;

        private ListView listViewPoi;
        private Button back;
        private Button add;
        private ListViewAdapter adapter;
        private Spinner spinnerSelection;
        private EditText search;
        private GpsLocation location = new GpsLocation();
        private double maxDistance;
        private double minAltitude;
        private List<PoiViewItem> items;
        private Timer searchTimer = new Timer();
        private PoiDatabase database;
        private String[] _listOfSelections = new String[] { "Visible points", "My points", "Find by name"};

        public PoiDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new PoiDatabase();
                }
                return database;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            location.Latitude = Intent.GetDoubleExtra("latitude", 0);
            location.Longitude = Intent.GetDoubleExtra("longitude", 0);
            location.Altitude = Intent.GetDoubleExtra("altitude", 0);
            
            //TODO: we can get minAltitude and maxDistance - not needed as activity parameter
            maxDistance = Intent.GetIntExtra("maxDistance", 0);
            minAltitude = Intent.GetIntExtra("minAltitude", 0);

            SetContentView(Resource.Layout.PoiListActivity);
            // Create your application here

            listViewPoi = FindViewById<ListView>(Resource.Id.listView1);
            listViewPoi.ItemClick += OnListItemClick;

            back = FindViewById<Button>(Resource.Id.buttonBack);
            back.SetOnClickListener(this);
            add = FindViewById<Button>(Resource.Id.buttonAdd);
            add.SetOnClickListener(this);
            search = FindViewById<EditText>(Resource.Id.editTextSearch);
            search.TextChanged += editTextSearch_TextChanged;

            spinnerSelection = FindViewById<Spinner>(Resource.Id.spinnerSelection);
            var selectionAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfSelections.ToList());
            spinnerSelection.Adapter = selectionAdapter;
            spinnerSelection.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(Selection_ItemSelected);

            _selectByDistance();

            InitializeSearchTimer();
        }

        private void InitializeSearchTimer()
        {
            searchTimer.Interval = 500;
            searchTimer.AutoReset = false;
            searchTimer.Elapsed += OnSearchTimerTimerElapsed;
        }

        private void OnSearchTimerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            searchTimer.Stop();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _FindItem();
            });
        }

        private void _FindItem()
        {
            var poiList = Database.FindItems(search.Text);

            items = new PoiViewItemList(poiList, location);
            items = items.OrderBy(i => i.Distance).ToList();
            adapter = new ListViewAdapter(this, items, this);
            listViewPoi.Adapter = adapter;
            listViewPoi.Invalidate();
        }

        private void editTextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            searchTimer.Start();
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
        }

        private void _selectByDistance()
        {
            //TODO: get minAltitude and maxDistance from CompassViewSettings
            var poiList = Database.GetItems(location, maxDistance);

            items = new PoiViewItemList(poiList, location, maxDistance, minAltitude, false);
            items = items.OrderBy(i => i.Distance).ToList();
            adapter = new ListViewAdapter(this, items, this);
            listViewPoi.Adapter = adapter;
            listViewPoi.Invalidate();
        }

        private void _selectMyPois()
        {
            var poiList = Database.GetMyItems();

            items = new PoiViewItemList(poiList, location);
            items = items.OrderBy(i => i.Distance).ToList();
            adapter = new ListViewAdapter(this, items, this);
            listViewPoi.Adapter = adapter;
            listViewPoi.Invalidate();
        }

        private void Selection_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            int selection = e.Position;
            if (selection == 0)
            {
                _selectByDistance();
            } else if (selection == 1)
            {
                _selectMyPois();
            }
            else if (selection == 2)
            {

            }
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
                case Resource.Id.buttonAdd:
                    OnPoiAdd();
                    break;
            }
        }

        public void OnPoiDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                PoiViewItem item = items[position];
                database.DeleteItemAsync(item.Poi);
                items.Remove(item);
                adapter = new ListViewAdapter(this, items, this);
                listViewPoi.Adapter = adapter;
            });
            alert.SetNegativeButton("No", (senderAlert, args) => { });
            alert.SetMessage("Are you sure you want to delete this item?");
            var answer = alert.Show();


            adapter.NotifyDataSetChanged();
        }

        public void OnPoiEdit(int position)
        {
            PoiViewItem item = items[position];
            Intent editActivityIntent = new Intent(this, typeof(EditActivity));
            editActivityIntent.PutExtra("Id", item.Poi.Id);
            StartActivity(editActivityIntent);

            adapter = new ListViewAdapter(this, items, this);
            listViewPoi.Adapter = adapter;
        }

        public void OnPoiAdd()
        {
            Intent editActivityIntent = new Intent(this, typeof(EditActivity));
            StartActivityForResult(editActivityIntent, ReqCode_AddPoiActivity);

            adapter = new ListViewAdapter(this, items, this);
            listViewPoi.Adapter = adapter;
        }


        public void OnPoiLike(int position)
        {
            PoiViewItem item = items[position];
            item.Poi.Favorite = !item.Poi.Favorite;
            adapter.NotifyDataSetChanged();
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