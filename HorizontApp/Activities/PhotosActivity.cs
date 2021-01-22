﻿using System;
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
using Android.Runtime;
using HorizontApp.Models;
using HorizontApp.Views.ListOfPoiView;

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
            AppContextLiveData.Instance.SetLocale(this);

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

            photoList = Context.PhotosModel.GetPhotoDataItems().OrderByDescending(x => x.Datetime).ToList();
            ShowPhotos(Context.ShowFavoritePicturesOnly);

            Context.PhotosModel.PhotoAdded += OnPhotoAdded;
            _photosListView.Adapter = _adapter;
        }

        private void OnPhotoAdded(object sender, PhotoAddedEventArgs args)
        {
            _adapter.Add(args.data);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PhotosActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
                case Resource.Id.menu_favourite:
                    Context.ToggleFavouritePictures();
                    ShowPhotos(Context.ShowFavoritePicturesOnly);
                    InvalidateOptionsMenu();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var buttonFavourite = menu.GetItem(0);
            buttonFavourite.SetIcon(Context.ShowFavoritePicturesOnly ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);
            
            return base.OnPrepareOptionsMenu(menu);
        }

        public void OnPhotoDelete(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                PhotoData item = _adapter[position];
                Context.Database.DeleteItem(item);
                _adapter.RemoveAt(position);
            });
            alert.SetNegativeButton("No", (senderAlert, args) => { });
            alert.SetMessage("Are you sure you want to delete this Photo?");
            var answer = alert.Show();

            _adapter.NotifyDataSetChanged();
        }

        public void OnPhotoEdit(int position)
        {
            Intent showIntent = new Intent(this, typeof(PhotoShowActivity));
            showIntent.PutExtra("ID", _adapter[position].Id);

            StartActivityForResult(showIntent, PhotoShowActivity.REQUEST_SHOW_PHOTO);
        }

        public void OnFavouriteEdit(int position)
        {
            PhotoData item = _adapter[position];
            item.Favourite = !item.Favourite;
            Context.Database.UpdateItem(item);

            _adapter.NotifyDataSetChanged();
        }

        public void OnTagEdit(int position)
        {   
            PhotoData item = _adapter[position];
            var dialog = new EditTagDialog(this, item);
            dialog.Show(r =>
            {
                if (r == Result.Ok)
                {
                    _adapter.NotifyDataSetChanged();
                }
            }); 
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PhotoShowActivity.REQUEST_SHOW_PHOTO)
            {
                if (data != null)
                {
                    var id = data.GetLongExtra("Id", -1);
                    var item = Context.Database.GetPhotoDataItem(id);
                    var photoItem = photoList.Single(p => p.Id == id);
                    photoItem.Heading = item.Heading;
                    _adapter.NotifyDataSetChanged();
                }
            }
        }

        private void ShowPhotos(bool favoriesOnly)
        {
            var list = photoList.AsQueryable();
            if (favoriesOnly)
            {
                list = list.Where(i => i.Favourite);
            }

            _adapter = new PhotosItemAdapter(this, list.OrderByDescending(i => i.Datetime), this);
            _photosListView.Adapter = _adapter;
        }
    }
}