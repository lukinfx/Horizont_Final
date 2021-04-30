using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Models;
using Xamarin.Essentials;

namespace Peaks360App.Utilities
{
    public interface IPhotoActionListener
    {
        void OnPhotoDeleteRequest(int position);
        void OnTagEditRequest(int position);
        void OnPhotoEditRequest(int position);
        void OnFavouriteEditRequest(int position);
    }

    [Activity(Label = "PhotosItemAdapter")]
    public class PhotosItemAdapter : BaseAdapter<PhotoData>, View.IOnClickListener
    {
        private Activity _context;
        private List<PhotoData> _list;
        private ImageView _thumbnailImageView;
        private IPhotoActionListener _poiActionListener;


        public PhotosItemAdapter(Activity context, IEnumerable<PhotoData> list, IPhotoActionListener listener) : base()
        {
            _context = context;
            _list = list.ToList();
            _poiActionListener = listener;
        }

        public override int Count
        {
            get { return _list.Count; }
        }

        public PhotoData GetById(long id)
        {
            return _list.SingleOrDefault(p => p.Id == id);
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override PhotoData this[int index]
        {
            get { return _list[index]; }
        }

        public int GetPosition(PhotoData item)
        {
            return _list.FindIndex(x => x.Id == item.Id);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            NotifyDataSetChanged();
        }

        public void Add(PhotoData item)
        {
            _list.Insert(0, item);
            NotifyDataSetChanged();
        }
        public void Update(PhotoData item)
        {
            var photoItem = GetById(item.Id);
            if (photoItem != null)
            {
                photoItem.Heading = item.Heading;
                NotifyDataSetChanged();
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
            {
                if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
                {
                    view = _context.LayoutInflater.Inflate(Resource.Layout.PhotosActivityItem, parent, false);
                }
                else
                {
                    view = _context.LayoutInflater.Inflate(Resource.Layout.PhotosActivityItemLandscape, parent, false);
                }
            }

            view.Tag = position;

            view.SetOnClickListener(this);
            PhotoData item = this[position];

            _thumbnailImageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail);

            view.FindViewById<TextView>(Resource.Id.textViewTag).Text = item.Tag;
            view.FindViewById<TextView>(Resource.Id.textViewDate).Text = item.Datetime.ToString();
            view.FindViewById<TextView>(Resource.Id.textViewAltitude).Text = $"{Math.Round(item.Altitude)} m | {Math.Round(item.Heading)}°"; 
            view.FindViewById<TextView>(Resource.Id.textViewLocation).Text = GpsUtils.LocationAsString(item.Latitude, item.Longitude);

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.photoDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

            var editButton = view.FindViewById<ImageButton>(Resource.Id.photoEditButton);
            editButton.SetOnClickListener(this);
            editButton.Tag = position;

            var favouriteButton = view.FindViewById<ImageButton>(Resource.Id.favouriteButton);
            favouriteButton.SetOnClickListener(this);
            favouriteButton.Tag = position;

            if (item.Favourite)
                favouriteButton.SetImageResource(Resource.Drawable.f_heart_solid);
            else
                favouriteButton.SetImageResource(Resource.Drawable.f_heart_empty);

            var path = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), item.PhotoFileName);

            if (item.Thumbnail != null)
            {
                var bitmap = BitmapFactory.DecodeByteArray(item.Thumbnail, 0, item.Thumbnail.Length);
                _thumbnailImageView.SetImageBitmap(bitmap);
            }

            return view;
        }

        public void OnClick(View v)
        {
            if (_poiActionListener == null)
                return;

            int position = (int)v.Tag;

            switch (v.Id)
            {
                case Resource.Id.photoDeleteButton:
                    _poiActionListener.OnPhotoDeleteRequest(position);
                    break;
                case Resource.Id.linearLayoutItem:
                    _poiActionListener.OnPhotoEditRequest(position);
                    break;
                case Resource.Id.photoEditButton:
                    _poiActionListener.OnTagEditRequest(position);
                    break;
                case Resource.Id.favouriteButton:
                    _poiActionListener.OnFavouriteEditRequest(position);
                    break;
            }
        }
    }
}