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
using static Android.Views.View;

namespace HorizontApp.Views.ListOfPoiView
{
    [Activity(Label = "PoiListActivity")]
    public class PoiListActivity : Activity, IOnClickListener
    {
        ListView listViewPoi;
        Button back;

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

            var items = new PoiViewItemList(poiList, location, maxDistance, minAltitude);

            
            var listAdapter = new ArrayAdapter<PoiViewItem>(this, Android.Resource.Layout.SimpleListItem1, items);
            listViewPoi.Adapter = listAdapter;
            foreach (var item in  CompassView.list)
            {
                 items.Add(item);
            }
        }
        
        public void OnClick(View v)
        {
            Finish();
        }
    }
}