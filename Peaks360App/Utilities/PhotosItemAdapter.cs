using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Utilities
{
    public interface IPhotoActionListener
    {
        void OnPhotoDelete(int position);
        void OnTagEdit(int position);
        void OnPhotoEdit(int position);
        void OnFavouriteEdit(int position);
    }

    [Activity(Label = "PhotosItemAdapter")]
    public class PhotosItemAdapter : BaseAdapter<PhotoData>, View.IOnClickListener
    {
        Activity context;
        List<PhotoData> list;
        private ImageView ThumbnailImageView;
        private IPhotoActionListener mPoiActionListener;


        public PhotosItemAdapter(Activity _context, IEnumerable<PhotoData> _list, IPhotoActionListener listener) : base()
        {
            this.context = _context;
            this.list = _list.ToList();
            mPoiActionListener = listener;
        }

        public override int Count
        {
            get { return list.Count; }
        }

        public PhotoData GetById(long id)
        {
            return list.SingleOrDefault(p => p.Id == id);
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override PhotoData this[int index]
        {
            get { return list[index]; }
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            NotifyDataSetChanged();
        }

        public void Add(PhotoData item)
        {
            list.Insert(0, item);
            NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.PhotosActivityItem, parent, false);

            view.Tag = position;

            view.SetOnClickListener(this);
            PhotoData item = this[position];
            view.FindViewById<TextView>(Resource.Id.textViewDate).Text = item.Tag + " | " +item.Datetime.ToString();
            view.FindViewById<TextView>(Resource.Id.textViewLocation).Text = $"{Math.Round(item.Altitude)} m | {Math.Round(item.Heading)}° | {item.Latitude:F6}/{item.Longitude:F6}";
            ThumbnailImageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail);

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
                ThumbnailImageView.SetImageBitmap(bitmap);
            }

            return view;
        }

        public void OnClick(View v)
        {
            if (mPoiActionListener == null)
                return;

            int position = (int)v.Tag;

            switch (v.Id)
            {
                case Resource.Id.photoDeleteButton:
                    mPoiActionListener.OnPhotoDelete(position);
                    break;

                case Resource.Id.linearLayoutItem:
                    mPoiActionListener.OnPhotoEdit(position);
                    break;
                case Resource.Id.photoEditButton:
                    mPoiActionListener.OnTagEdit(position);
                    break;
                case Resource.Id.favouriteButton:
                    mPoiActionListener.OnFavouriteEdit(position);
                    break;
            }
        }
    }
}