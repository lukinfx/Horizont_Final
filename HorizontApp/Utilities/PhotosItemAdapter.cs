using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Views.Camera;
using HorizontLib.Domain.Models;
using Java.IO;

namespace HorizontApp.Utilities
{
    public interface IPhotoActionListener
    {
        void OnPhotoDelete(int position);

        void OnPhotoEdit(int position);
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

        public override long GetItemId(int position)
        {
            return position;
        }

        public override PhotoData this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.PhotosActivityItem, parent, false);

            view.SetOnClickListener(this);
            PhotoData item = this[position];
            view.FindViewById<TextView>(Resource.Id.textViewDate).Text = item.Tag + " | " +item.Datetime.ToString();
            view.FindViewById<TextView>(Resource.Id.textViewLocation).Text = Math.Round(item.Altitude) + " m" + " | " + item.Heading + "°";
            ThumbnailImageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail);
            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.photoDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

            var path = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), item.PhotoFileName);

            
            
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
            }
        }
    }
}