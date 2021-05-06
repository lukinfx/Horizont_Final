using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
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
        Activity _activity;
        List<PoiViewItem> _list;
        private ImageView _thumbnail;
        private IPoiActionListener _poiActionListener;
        private bool _showOptions;

        public PoiListItemAdapter(Activity activity, IEnumerable<PoiViewItem> list, IPoiActionListener listener, bool showOptions = true)
            : base()
        {
            this._activity = activity;
            this._list = list.ToList();
            _poiActionListener = listener;
            _showOptions = showOptions;
        }

        public void SetItems(IEnumerable<PoiViewItem> list)
        {
            this._list = list.ToList();
            NotifyDataSetChanged();
        }

        public override int Count
        {
            get { return _list.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public PoiViewItem GetPoiItem(long id)
        {
            return _list.Single(x => x.Poi.Id == id);
        }

        public override PoiViewItem this[int index]
        {
            get { return _list[index]; }
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            NotifyDataSetChanged();
        }

        public void Add(PoiViewItem item)
        {
            _list.Insert(0,item);
            NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
            {
                view = _activity.LayoutInflater.Inflate(Resource.Layout.PoiListItemLayout, parent, false);
            }
            
            view.Tag = position;
            view.SetOnClickListener(this);

            PoiViewItem item = this[position];

            view.FindViewById<ImageView>(Resource.Id.InfoAvailable).Visibility = 
                (String.IsNullOrEmpty(item.Poi.Wikidata) && string.IsNullOrEmpty(item.Poi.Wikipedia)) 
                    ? ViewStates.Invisible : ViewStates.Visible;

            view.FindViewById<TextView>(Resource.Id.Title).Text = item.Poi.Name;

            view.FindViewById<TextView>(Resource.Id.Country).Text = item.Poi.Country != null ? 
                PoiCountryHelper.GetCountryName(item.Poi.Country.Value) : "Unknown";

            view.FindViewById<TextView>(Resource.Id.Description).Text = 
                $"{item.Poi.Altitude:F0} m | {item.GpsLocation.Distance.Value/1000f:F2} km | {item.GpsLocation.Bearing.Value:F2}°";

            _thumbnail = view.FindViewById<ImageView>(Resource.Id.Thumbnail);
            _thumbnail.SetColorFilter(ImageViewHelper.GetImportanceColorFilter(item.Poi));
            _thumbnail.SetImageResource(PoiCategoryHelper.GetImage(item.Poi.Category));

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.PoiDeleteButton);
            var likeButton = view.FindViewById<ImageButton>(Resource.Id.PoiLikeButton);

            if (_showOptions)
            {
                deleteButton.SetOnClickListener(this);
                deleteButton.Tag = position;

                likeButton.SetOnClickListener(this);
                likeButton.Tag = position;

                likeButton.SetImageResource(item.Poi.Favorite ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);
            }
            else
            {
                deleteButton.Visibility = ViewStates.Gone;
                likeButton.Visibility = ViewStates.Gone;
            }

            view.SetBackgroundResource(Resource.Drawable.bg_activity);
            view.Background.Alpha = item.Selected ? 100 : 0;
            return view;
        }

        public void OnClick(View v)
        {
            if (_poiActionListener == null)
                return;

            int position = (int)v.Tag;

            switch (v.Id)
            {
                case Resource.Id.PoiDeleteButton:
                    _poiActionListener.OnPoiDelete(position);
                    break;
                case Resource.Id.PoiLikeButton:
                    _poiActionListener.OnPoiLike(position);
                    break;
                case Resource.Id.linearLayoutItem:
                    _poiActionListener.OnPoiEdit(position);
                    break;
            }
        }
    }
}