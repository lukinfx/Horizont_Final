using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Enums;
using HorizontApp.Domain.ViewModel;

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
        TextView Favourite;
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
                view = context.LayoutInflater.Inflate(Resource.Layout.ListItemLayout, parent, false);

            PoiViewItem item = this[position];
            view.FindViewById<TextView>(Resource.Id.Title).Text = item.Poi.Name;
            view.FindViewById<TextView>(Resource.Id.Description).Text = Convert.ToString(item.Poi.Altitude) + "m | " + Convert.ToString(Math.Round(item.Distance/1000, 2)) + " km";

            Favourite = view.FindViewById<TextView>(Resource.Id.Favourite);
            if (item.Poi.Favorite)
                Favourite.Text = "❤";
            else 
                Favourite.Text = "♡";

            Thumbnail = view.FindViewById<ImageView>(Resource.Id.Thumbnail);
            Thumbnail.SetImageResource(GetImage(item));

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.PoiDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

            var editButton = view.FindViewById<ImageButton>(Resource.Id.PoiEditButton);
            editButton.SetOnClickListener(this);
            editButton.Tag = position;

            var likeButton = view.FindViewById<ImageButton>(Resource.Id.PoiLikeButton);
            likeButton.SetOnClickListener(this);
            likeButton.Tag = position;

            return view;
        }

        public int GetImage(PoiViewItem item)
        { 
            switch (item.Poi.Category) 
            { 
                case PoiCategory.Castles: 
                    return Resource.Drawable.c_castle; 
                case PoiCategory.Mountains: 
                    return Resource.Drawable.c_mountain; 
                case PoiCategory.Lakes: 
                    return Resource.Drawable.c_lake; 
                case PoiCategory.ViewTowers: 
                    return Resource.Drawable.c_viewtower; 
                case PoiCategory.Palaces: 
                    return Resource.Drawable.c_palace; 
                case PoiCategory.Ruins: 
                    return Resource.Drawable.c_ruins; 
                case PoiCategory.Transmitters: 
                    return Resource.Drawable.c_transmitter;
                case PoiCategory.Churches:
                    return Resource.Drawable.c_church; 
                default: 
                    return Resource.Drawable.c_basic;
            }
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
                case Resource.Id.PoiEditButton:
                    mPoiActionListener.OnPoiEdit(position);
                    break;
                case Resource.Id.PoiLikeButton:
                    mPoiActionListener.OnPoiLike(position);
                    break;
            }
        }
    }
}