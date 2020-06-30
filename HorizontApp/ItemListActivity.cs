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

//Example from
//https://stacktips.com/tutorials/xamarin/listview-example-in-xamarin-android

namespace HorizontApp
{
    [Activity(Label = "ItemListActivity")]
    public class ItemListActivity : Activity
    {
        private ListView listView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Xamarin.Essentials.Platform.Init(this, bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.ItemListActivityLayout);

            listView = (ListView)FindViewById(Resource.Id.ListView);
            //listView.ItemClick += OnListItemClick;
            var l = new List<Post>()
            {
                new Post() {description = "desc aaa", title = "aaa"},
                new Post() {description = "desc bbb", title = "bbb"},
                new Post() {description = "desc ccc", title = "ccc"},
            };
            listView.Adapter = new ItemListAdapter(this, l);
        }
    }
}