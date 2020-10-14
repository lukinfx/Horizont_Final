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
using HorizontLib.Domain.Models;

namespace HorizontApp.Utilities
{
    public interface IPhotoActionListener
    {
        void OnPoiDelete(int position);
    }

    [Activity(Label = "PhotosItemAdapter")]
    public class PhotosItemAdapter : BaseAdapter<PhotoData>, View.IOnClickListener
    {
        Activity context;
        List<PhotoData> list;
        private ImageView Thumbnail;
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

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.photoDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

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
                    mPoiActionListener.OnPoiDelete(position);
                    break;
            }
        }
    }
}