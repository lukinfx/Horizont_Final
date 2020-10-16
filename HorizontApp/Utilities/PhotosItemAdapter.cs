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
            view.FindViewById<TextView>(Resource.Id.textViewDate).Text = item.Datetime.ToString();
            view.FindViewById<TextView>(Resource.Id.textViewLocation).Text = item.Altitude + " m";
            ThumbnailImageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail);
            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.photoDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

            var path = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), item.PhotoFileName);

            try
            {
                //Thumbnail.SetImageBitmap(bmp);
                using (FileStream fs = System.IO.File.OpenRead(path))
                {
                    //var pic = Picture.CreateFromStream(fs);
                    //Bitmap.CreateBitmap(pic);
                    var bitmap = BitmapFactory.DecodeStream(fs);
                    var bitmapScalled = Bitmap.CreateScaledBitmap(bitmap, 150, 100, true);
                    ThumbnailImageView.SetImageBitmap(bitmapScalled);

                    //var thumbnail = ImageResizer.BitmapToThumbnail(bitmap);
                    //ThumbnailImageView.SetImageResource(thumbnail.GetHashCode());
                }
            }
            catch (Exception)
            { 
            }
                
            /*Bitmap.CreateBitmap()
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(path);*/
            
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
            }
        }
    }
}