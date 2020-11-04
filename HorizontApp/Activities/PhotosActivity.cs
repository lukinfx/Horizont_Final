using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using HorizontApp.AppContext;
using HorizontApp.Utilities;
using Android.Content;
using System.Collections.Generic;
using HorizontLib.Domain.Models;
using System.Linq;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotosActivity")]
    public class PhotosActivity : Activity, IPhotoActionListener
    {
        private ListView _photosListView;
        private PhotosItemAdapter _adapter;
        private List<PhotoData> photoList;
        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.PhotosActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.PhotosActivityLandscape);
            }

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(false);

            _photosListView = FindViewById<ListView>(Resource.Id.listViewPhotos);

            photoList = Context.Database.GetPhotoDataItems().ToList();
            _adapter = new PhotosItemAdapter(this, photoList, this);

            _photosListView.Adapter = _adapter;
        }

        void OnPhotoShow(int position)
        {
            Intent showIntent = new Intent(this, typeof(PhotoShowActivity));
            showIntent.PutExtra("ID", photoList[position].Id);

            StartActivity(showIntent);
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

        public void OnPhotoDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                PhotoData item = photoList[position];
                Context.Database.DeleteItem(item);
                photoList.Remove(item);
                _adapter = new PhotosItemAdapter(this, photoList, this);
                _photosListView.Adapter = _adapter;
            });
            alert.SetNegativeButton("No", (senderAlert, args) => { });
            alert.SetMessage("Are you sure you want to delete this Photo?");
            var answer = alert.Show();


            _adapter.NotifyDataSetChanged();
        }

        public void OnPhotoEdit(int position)
        {
            OnPhotoShow(position);
        }
        public void OnTagEdit(int position)
        {   
            PhotoData item = photoList[position];
            var dialog = new EditTagDialog(this, item);
            dialog.Show(); 
            //dialog.OnActionModeFinished(OnTagEditFinished);
        }
        /*private void OnTagEditFinished(ActionMode am)
        {
            photoList = Context.Database.GetPhotoDataItems().ToList();
            _adapter = new PhotosItemAdapter(this, photoList, this);
        }*/
    }
}