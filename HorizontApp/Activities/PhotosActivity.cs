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

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotosActivity")]
    public class PhotosActivity : Activity
    {
        private PoiDatabase _database;
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
            // Create your application here
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
    }
}