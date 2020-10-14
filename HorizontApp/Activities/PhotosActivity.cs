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
using HorizontApp.Utilities;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotosActivity")]
    public class PhotosActivity : Activity, IPhotoActionListener
    {
        private ListView _photosListView;
        private PhotosItemAdapter _adapter;
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
            SetContentView(Resource.Layout.PhotosActivity);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(false);

            _photosListView = FindViewById<ListView>(Resource.Id.listViewPhotos);

            var photoList = Database.GetPhotoDataItems();
            _adapter = new PhotosItemAdapter(this, photoList, this);

            _photosListView.Adapter = _adapter;
            _photosListView.ItemClick += OnListItemClick;
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            OnPhotoShow(e.Position);
        }

        void OnPhotoShow(int position)
        {

        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void OnPoiDelete(int position)
        {
            throw new NotImplementedException();
        }
    }
}