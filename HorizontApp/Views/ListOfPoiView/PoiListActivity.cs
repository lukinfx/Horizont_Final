using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.DataAccess;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;

using HorizontApp.Utilities;
using static Android.Views.View;

namespace HorizontApp.Views.ListOfPoiView
{
    [Activity(Label = "PoiListActivity")]
    public class PoiListActivity : Activity, IOnClickListener
    {
        ListView listViewPoi;
        Button back;
        ListViewAdapter adapter;
        private List<PoiViewItem> items;
        private PoiDatabase database;
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

            var location = new GpsLocation()
            {
                Latitude = Intent.GetDoubleExtra("latitude", 0),
                Longitude = Intent.GetDoubleExtra("longitude", 0),
                Altitude = Intent.GetDoubleExtra("altitude", 0),
            };

            var maxDistance = Intent.GetIntExtra("maxDistance", 0);
            var minAltitude = Intent.GetIntExtra("minAltitude", 0);

            SetContentView(Resource.Layout.PoiListActivity);
            // Create your application here

            listViewPoi = FindViewById<ListView>(Resource.Id.listView1);
            
            back = FindViewById<Button>(Resource.Id.button1);
            back.SetOnClickListener(this);

            var poiList = Database.GetItems();

            items = new PoiViewItemList(poiList, location, maxDistance, minAltitude);
            items = items.OrderBy(i => i.Distance).ToList();

            adapter = new ListViewAdapter(this, items);
            listViewPoi.Adapter = adapter;
            listViewPoi.ItemClick += OnListItemClick;
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            PoiViewItem item = items[e.Position];
            item.Poi.Favorite = !item.Poi.Favorite;

            adapter.NotifyDataSetChanged();

            //var itemToUpdate = new Poi()
            //{
            //    Id = item.Id,
            //    Name = item.Name,
            //    Category = item.Category,
            //    Altitude = item.Altitude,
            //    Latitude = item.Latitude,
            //    Longitude = item.Longitude,
            //    Favorite = item.Favorite
            //};
            Database.UpdateItemAsync(item.Poi);
        }

        public void OnClick(View v)
        {
            Finish();
        }
    }
}