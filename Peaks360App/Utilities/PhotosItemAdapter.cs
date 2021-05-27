using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Peaks360App.Extensions;
using Xamarin.Essentials;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Utilities
{
    public interface IPhotoActionListener
    {
        void OnPhotoDeleteRequest(int position);
        void OnTagEditRequest(int position);
        void OnPhotoEditRequest(int position);
        void OnPhotoShowRequest(int position);
        void OnFavouriteEditRequest(int position);
    }

    [Activity(Label = "PhotosItemAdapter")]
    public class PhotosItemAdapter : BaseAdapter<PhotoData>, View.IOnClickListener
    {
        private Activity _context;
        private List<PhotoData> _list;
        private IPhotoActionListener _poiActionListener;


        public PhotosItemAdapter(Activity context, IEnumerable<PhotoData> list, IPhotoActionListener listener) : base()
        {
            _context = context;
            _list = list.ToList();
            _poiActionListener = listener;
        }

        public void SetItems(IEnumerable<PhotoData> list)
        {
            this._list = list.OrderByDescending(x => x.GetPhotoTakenDateTime()).ThenByDescending(x => x.Datetime).ToList();
            NotifyDataSetChanged();
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
            get { return (index < 0 || index >= _list.Count) ? (PhotoData) null : _list[index]; }
        }

        public int GetPosition(PhotoData item)
        {
            return _list.FindIndex(x => x.Id == item.Id);
        }

        public PhotoData GetNextPhotoDataItem(PhotoData current)
        {
            int pos = GetPosition(current);
            return this[++pos];
        }
        
        public PhotoData GetPreviousPhotoDataItem(PhotoData current)
        {
            int pos = GetPosition(current);
            return this[--pos];
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            NotifyDataSetChanged();
        }

        public void Add(PhotoData item)
        {
            _list.Insert(0, item);
            _list = _list.OrderByDescending(x => x.GetPhotoTakenDateTime()).ThenByDescending(x => x.Datetime).ToList();

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

            PhotoData item = this[position];
            var gpsLocation = item.GetPhotoGpsLocation();

            view.FindViewById<TextView>(Resource.Id.textViewTag).Text = item.Tag;
            view.FindViewById<TextView>(Resource.Id.textViewDate).Text = item.GetPhotoTakenDateTime().ToShortDateString();
            view.FindViewById<TextView>(Resource.Id.textViewTime).Text = item.GetPhotoTakenDateTime().ToLongTimeString();
            view.FindViewById<TextView>(Resource.Id.textViewAltitude).Text = $"{Math.Round(item.Altitude)}m ";
            view.FindViewById<TextView>(Resource.Id.textViewAltitude).SetTextColor(GpsUtils.HasAltitude(gpsLocation) ? Color.Black : Color.Red);
            view.FindViewById<TextView>(Resource.Id.textViewDirection).Text = item.Heading.HasValue ? $"{Math.Round(GpsUtils.Normalize360(item.Heading.Value))}°" : "0°";
            view.FindViewById<TextView>(Resource.Id.textViewDirection).SetTextColor(item.Heading.HasValue ? Color.Black : Color.Red);
            view.FindViewById<TextView>(Resource.Id.textViewLocation).Text = gpsLocation.LocationAsShortString();
            view.FindViewById<TextView>(Resource.Id.textViewLocation).SetTextColor(GpsUtils.HasLocation(gpsLocation) ? Color.Black : Color.Red);

            var linearLayoutThumbnail = view.FindViewById<LinearLayout>(Resource.Id.linearLayoutThumbnail);
            linearLayoutThumbnail.SetOnClickListener(this);
            linearLayoutThumbnail.Tag = position;

            var linearLayoutName = view.FindViewById<LinearLayout>(Resource.Id.linearLayoutName);
            linearLayoutName.SetOnClickListener(this);
            linearLayoutName.Tag = position;

            var linearLayoutPhotoData = view.FindViewById<LinearLayout>(Resource.Id.linearLayoutPhotoData);
            linearLayoutPhotoData.SetOnClickListener(this);
            linearLayoutPhotoData.Tag = position;

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.photoDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;

            var editButton = view.FindViewById<ImageButton>(Resource.Id.editButton);
            editButton.SetOnClickListener(this);
            editButton.Tag = position;

            var favouriteButton = view.FindViewById<ImageButton>(Resource.Id.favouriteButton);
            favouriteButton.SetOnClickListener(this);
            favouriteButton.Tag = position;
            favouriteButton.SetImageResource(item.Favourite ? Android.Resource.Drawable.ButtonStarBigOn : Android.Resource.Drawable.ButtonStarBigOff);

            if (item.Thumbnail != null)
            {
                var bitmap = BitmapFactory.DecodeByteArray(item.Thumbnail, 0, item.Thumbnail.Length);
                view.FindViewById<ImageView>(Resource.Id.Thumbnail).SetImageBitmap(bitmap);
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
                case Resource.Id.editButton:
                case Resource.Id.linearLayoutPhotoData:
                    _poiActionListener.OnPhotoEditRequest(position);
                    break;
                case Resource.Id.linearLayoutThumbnail:
                    _poiActionListener.OnPhotoShowRequest(position);
                    break;
                case Resource.Id.linearLayoutName:
                    _poiActionListener.OnTagEditRequest(position);
                    break;
                case Resource.Id.favouriteButton:
                    _poiActionListener.OnFavouriteEditRequest(position);
                    break;
            }
        }
    }
}