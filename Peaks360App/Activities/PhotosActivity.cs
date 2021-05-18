﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Android.Content;
using Android.Provider;
using Xamarin.Essentials;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using Peaks360App.Models;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/PhotosActivity")]
    public class PhotosActivity : Activity, IPhotoActionListener
    {
        public static int REQUEST_IMPORT_PHOTO = Definitions.BaseResultCode.PHOTOS_ACTIVITY;
        
        private ListView _photosListView;
        private PhotosItemAdapter _adapter;
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
            ActionBar.SetDisplayShowTitleEnabled(true);
            ActionBar.SetTitle(Resource.String.PhotosActivity);

            _photosListView = FindViewById<ListView>(Resource.Id.listViewPhotos);
            _adapter = new PhotosItemAdapter(this, new List<PhotoData>(), this);
            _photosListView.Adapter = _adapter;

            ShowPhotos(Context.ShowFavoritePicturesOnly);

            Context.PhotosModel.PhotoAdded += OnPhotoAdded;
            Context.PhotosModel.PhotoUpdated += OnPhotoUpdated;
            Context.PhotosModel.PhotoDeleted += OnPhotoDeleted;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Context.PhotosModel.PhotoAdded -= OnPhotoAdded;
            Context.PhotosModel.PhotoUpdated -= OnPhotoUpdated;
            Context.PhotosModel.PhotoDeleted -= OnPhotoDeleted;
        }

        private void OnPhotoAdded(object sender, PhotoDataEventArgs args)
        {
            _adapter.Add(args.data);
        }

        private void OnPhotoUpdated(object sender, PhotoDataEventArgs args)
        {
            _adapter.Update(args.data);
        }

        private void OnPhotoDeleted(object sender, PhotoDataEventArgs args)
        {
            var position = _adapter.GetPosition(args.data);
            _adapter.RemoveAt(position);
        }
        
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PhotosActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public void InitializeMediaPicker()
        {
            Intent = new Intent();
            Intent.SetType("image/*");
            Intent.AddCategory(Intent.CategoryOpenable);
            Intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(Intent, "Select Picture"), REQUEST_IMPORT_PHOTO);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
                case Resource.Id.menu_addNew:
                    InitializeMediaPicker();
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
            buttonFavourite.SetIcon(Context.ShowFavoritePicturesOnly ? Android.Resource.Drawable.ButtonStarBigOn : Android.Resource.Drawable.ButtonStarBigOff);
            
            return base.OnPrepareOptionsMenu(menu);
        }

        public void OnPhotoDeleteRequest(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
            alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
            {
                PhotoData item = _adapter[position];
                Context.PhotosModel.DeleteItem(item);
            });
            alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
            alert.SetMessage(Resources.GetText(Resource.String.Photos_DeletePhotoQuestion));
            var answer = alert.Show();
        }

        public void OnPhotoEditRequest(int position)
        {
            Intent showIntent = new Intent(this, typeof(PhotoShowActivity));
            showIntent.PutExtra("ID", _adapter[position].Id);

            StartActivity(showIntent);
        }

        public void OnFavouriteEditRequest(int position)
        {
            PhotoData item = _adapter[position];
            item.Favourite = !item.Favourite;
            Context.Database.UpdateItem(item);

            _adapter.NotifyDataSetChanged();
        }

        public void OnTagEditRequest(int position)
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
            if (resultCode == Result.Ok && requestCode == REQUEST_IMPORT_PHOTO)
            {
                var uri = data.Data;

                try
                {
                    ImageSaverCopy imageSaver = new ImageSaverCopy(this, uri, Context);
                    imageSaver.Run();
                }
                catch (Exception ex)
                {
                    PopupHelper.ErrorDialog(this, "Unable to import image. Error details: " + ex.Message);
                }
            }
        }

        private void ShowPhotos(bool favoriesOnly)
        {
            var list = Context.PhotosModel.GetPhotoDataItems()
                .OrderByDescending(x => x.Datetime)
                .AsQueryable();

            if (favoriesOnly)
            {
                list = list.Where(i => i.Favourite);
            }
            _adapter.SetItems(list);
        }
    }
}