using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Android.Content;
using Xamarin.Essentials;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Peaks360App.Extensions;
using Peaks360App.Utilities;
using Peaks360App.Models;
using Peaks360Lib.Utilities;
using GpsUtils = Peaks360Lib.Utilities.GpsUtils;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/PhotosActivity")]
    public class PhotosActivity : Activity, IPhotoActionListener, SearchView.IOnQueryTextListener
    {
        public static int REQUEST_IMPORT_PHOTO = Definitions.BaseResultCode.PHOTOS_ACTIVITY;
        
        private ListView _photosListView;
        private SearchView _searchViewText;
        private LinearLayout _searchViewLayout;
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
            Context.PhotosItemAdapter = _adapter;

            _searchViewLayout = FindViewById<LinearLayout>(Resource.Id.searchViewLayout);
            _searchViewLayout.Visibility = ViewStates.Gone;

            _searchViewText = FindViewById<SearchView>(Resource.Id.searchViewPhotoName);
            _searchViewText.Iconified = false;
            _searchViewText.SetQueryHint(Resources.GetText(Resource.String.Common_Search));
            _searchViewText.SetOnQueryTextListener(this);
            _searchViewText.ClearFocus();

            ShowPhotos(Context.ShowFavoritePicturesOnly, GetSearchText());

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
            var position = _adapter.GetPosition(args.data);
            _photosListView.SetSelection(position);
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
                case Resource.Id.menu_search:
                    ToogleSearch();
                    break;
                case Resource.Id.menu_addNew:
                    InitializeMediaPicker();
                    break;
                case Resource.Id.menu_favourite:
                    Context.ToggleFavouritePictures();
                    ShowPhotos(Context.ShowFavoritePicturesOnly, GetSearchText());
                    InvalidateOptionsMenu();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void ToogleSearch()
        {
            _searchViewLayout.Visibility = _searchViewLayout.Visibility == ViewStates.Visible ? ViewStates.Gone : ViewStates.Visible;
            ShowPhotos(Context.ShowFavoritePicturesOnly, GetSearchText());
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            var buttonFavourite = menu.FindItem(Resource.Id.menu_favourite);
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

        public void OnPhotoShowRequest(int position)
        {
            var photoData = _adapter[position];
            if (!GpsUtils.HasLocation(photoData.GetPhotoGpsLocation()) || !GpsUtils.HasAltitude(photoData.GetPhotoGpsLocation()) || !photoData.Heading.HasValue)
            {
                PopupHelper.ErrorDialog(this, "Set camera location and view direction first.");
                return;
            }
            Intent showIntent = new Intent(this, typeof(PhotoShowActivity));
            showIntent.PutExtra("ID", _adapter[position].Id);
            StartActivity(showIntent);
        }

        public void OnPhotoEditRequest(int position)
        {
            Intent importActivityIntent = new Intent(this, typeof(PhotoImportActivity));
            importActivityIntent.PutExtra("Id", _adapter[position].Id);
            StartActivity(importActivityIntent);
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

                var path = PathUtil.GetPath(this, uri);
                if (path == null)
                {
                    PopupHelper.ErrorDialog(this, "Unable to load image. Download image into your device first.");
                    return;
                }

                var exifData = ExifDataReader.ReadExifData(path);


                //update altitude
                if (Peaks360Lib.Utilities.GpsUtils.HasLocation(exifData.location))
                {
                    if (!Peaks360Lib.Utilities.GpsUtils.HasAltitude(exifData.location))
                    {
                        if (TryGetElevation(exifData.location, out var altitude))
                        {
                            exifData.location.Altitude = altitude;
                        }
                    }
                }

                Task.Run(async () =>
                {
                    var photoData = await ImageSaver.Import(path, exifData, Context);

                    Intent importActivityIntent = new Intent(this, typeof(PhotoImportActivity));
                    importActivityIntent.PutExtra("Id", photoData.Id);
                    StartActivity(importActivityIntent);
                });

            }
        }

        private void ShowPhotos(bool favoriesOnly, string searchText)
        {
            var list = Context.PhotosModel.GetPhotoDataItems()
                .AsQueryable();

            if (favoriesOnly)
            {
                list = list.Where(i => i.Favourite);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                var temp = searchText.RemoveDiacritics().ToLower();
                list = list.Where(i => i.Tag.RemoveDiacritics().ToLower().Contains(temp));
            }

            _adapter.SetItems(list);
        }

        private bool TryGetElevation(GpsLocation location, out double altitude)
        {
            var et = new ElevationTile(location);
            if (et.Exists())
            {
                if (et.LoadFromZip())
                {
                    altitude = et.GetElevation(location);
                    return true;
                }
            }

            altitude = 0;
            return false;
        }

        public bool OnQueryTextChange(string newText)
        {
            ShowPhotos(Context.ShowFavoritePicturesOnly, GetSearchText());
            return true;
        }

        public bool OnQueryTextSubmit(string query)
        {
            _searchViewText.ClearFocus();
            return true;
        }

        private string GetSearchText()
        {
            return _searchViewLayout.Visibility == ViewStates.Visible ? _searchViewText.Query : null;
        }

    }
}