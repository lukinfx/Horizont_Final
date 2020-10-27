using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using HorizontLib.Domain.ViewModel;

namespace HorizontApp.Utilities
{
    public interface IPoiActionListener
    {
        void OnPoiDelete(int position);
        void OnPoiEdit(int position);
        void OnPoiLike(int position);
    }

    public class ListViewAdapter : BaseAdapter<PoiViewItem>, View.IOnClickListener
    {
        Activity context;
        List<PoiViewItem> list;
        private ImageView Thumbnail;
        private IPoiActionListener mPoiActionListener;

        public ListViewAdapter(Activity _context, IEnumerable<PoiViewItem> _list, IPoiActionListener listener)
            : base()
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

        public override PoiViewItem this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.PoiListItemLayout, parent, false);
            
            view.Tag = position;

            view.SetOnClickListener(this);

            PoiViewItem item = this[position];
            view.FindViewById<TextView>(Resource.Id.Title).Text = item.Poi.Name;
            view.FindViewById<TextView>(Resource.Id.Description).Text = Convert.ToString(item.Poi.Altitude) + "m | " + 
                Convert.ToString(Math.Round(item.Distance/1000, 2)) + " km |" +
                Convert.ToString(Math.Round(item.Bearing, 2)) + " dg";


            Thumbnail = view.FindViewById<ImageView>(Resource.Id.Thumbnail);
            Thumbnail.SetImageResource(PoiCategoryHelper.GetImage(item.Poi.Category));

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.PoiDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

            var likeButton = view.FindViewById<ImageButton>(Resource.Id.PoiLikeButton);
            likeButton.SetOnClickListener(this);
            likeButton.Tag = position;


            if (item.Poi.Favorite)
                likeButton.SetImageResource(Resource.Drawable.f_heart_solid);
            else
                likeButton.SetImageResource(Resource.Drawable.f_heart_empty);

            return view;
        }

        public void OnClick(View v)
        {
            if (mPoiActionListener == null)
                return;

            int position = (int)v.Tag;

            switch (v.Id)
            {
                case Resource.Id.PoiDeleteButton:
                    mPoiActionListener.OnPoiDelete(position);
                    break;
                case Resource.Id.PoiLikeButton:
                    mPoiActionListener.OnPoiLike(position);
                    break;
                case Resource.Id.linearLayoutItem:
                    mPoiActionListener.OnPoiEdit(position);
                    break;


            }
        }
    }
}