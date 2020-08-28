using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using HorizontApp.Activities;
using HorizontApp.DataAccess;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;

using HorizontApp.Utilities;
using static Android.Views.View;

namespace HorizontApp.Views.ListOfPoiView
{
    [Activity(Label = "PoiListActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class PoiListActivity : Activity, IOnClickListener, IPoiActionListener
    {
        ListView listViewPoi;
        Button back;
        Button add;
        ListViewAdapter adapter;
        Spinner spinnerSelection;
        GpsLocation location = new GpsLocation();
        double maxDistance;
        double minAltitude;
        private List<PoiViewItem> items;
        private PoiDatabase database;
        private String[] _listOfSelections = new String[] { "Serad podle vzdalenosti", "Zobraz mnou pridane body", "Najdi podle jmena"};

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

            spinnerSelection = FindViewById<Spinner>(Resource.Id.spinnerSelection);
            var selectionAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _listOfSelections.ToList());
            spinnerSelection.Adapter = selectionAdapter;
            spinnerSelection.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(Selection_ItemSelected);

            _selectByDistance();
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            
        }

        private void _selectByDistance()
        {
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

            items = new PoiViewItemList(poiList, location, 10000000, 0, false);
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
            StartActivity(editActivityIntent);

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
    }
}