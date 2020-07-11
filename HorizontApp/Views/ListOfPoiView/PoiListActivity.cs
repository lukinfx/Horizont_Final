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
    public class PoiListActivity : Activity, IOnClickListener, IPoiActionListener
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

            items = new PoiViewItemList(poiList, location, maxDistance, minAltitude, false);
            items = items.OrderBy(i => i.Distance).ToList();

            adapter = new ListViewAdapter(this, items, this);
            listViewPoi.Adapter = adapter;
            listViewPoi.ItemClick += OnListItemClick;
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            
        }

        public void OnClick(View v)
        {
            Finish();
        }

        public void OnPoiDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                PoiViewItem item = items[position];
                database.DeleteItemAsync(item.Poi);
            });
            alert.SetNegativeButton("No", (senderAlert, args) => { });
            alert.SetMessage("Are you sure you want to delete this item?");
            var answer = alert.Show();


            adapter.NotifyDataSetChanged();
        }

        public void OnPoiEdit(int position)
        {

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