﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Utilities
{
    public interface IPoiActionListener
    {
        void OnPoiDelete(int position);
        void OnPoiEdit(int position);
        void OnPoiLike(int position);
    }

    public class PoiListItemAdapter : BaseAdapter<PoiViewItem>, View.IOnClickListener
    {
        Activity context;
        List<PoiViewItem> list;
        private ImageView Thumbnail;
        private IPoiActionListener mPoiActionListener;

        public PoiListItemAdapter(Activity _context, IEnumerable<PoiViewItem> _list, IPoiActionListener listener)
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

        public PoiViewItem GetPoiItem(long id)
        {
            return list.Single(x => x.Poi.Id == id);
        }

        public override PoiViewItem this[int index]
        {
            get { return list[index]; }
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            NotifyDataSetChanged();
        }

        public void Add(PoiViewItem item)
        {
            list.Insert(0,item);
            NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.PoiListItemLayout, parent, false);
            
            view.Tag = position;

            view.SetOnClickListener(this);

            PoiViewItem item = this[position];
            
            view.FindViewById<ImageView>(Resource.Id.InfoAvailable).Visibility = 
                (String.IsNullOrEmpty(item.Poi.Wikidata) && string.IsNullOrEmpty(item.Poi.Wikipedia)) 
                    ? ViewStates.Invisible : ViewStates.Visible;
            
            view.FindViewById<TextView>(Resource.Id.Title).Text = item.Poi.Name;
            view.FindViewById<TextView>(Resource.Id.Description).Text = Convert.ToString(item.Poi.Altitude) + "m | " + 
                Convert.ToString(Math.Round(item.GpsLocation.Distance.Value /1000, 2)) + " km |" +
                Convert.ToString(Math.Round(item.GpsLocation.Bearing.Value, 2)) + " dg";


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