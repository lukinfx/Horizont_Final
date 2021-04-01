using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Xamarin.Essentials;

namespace Peaks360App.Utilities
{
    public interface IDownloadedElevationDataActionListener
    {
        void OnDedDeleteRequest(int position);
        void OnDedEditRequest(int position);
    }

    public class DownloadedElevationDataAdapter : BaseAdapter<DownloadedElevationData>, View.IOnClickListener
    {
        private Activity _context;
        private List<DownloadedElevationData> _list;
        private IDownloadedElevationDataActionListener _actionListener;

        public DownloadedElevationDataAdapter(Activity context, IDownloadedElevationDataActionListener actionListener)
            : base()
        {
            _context = context;
            _actionListener = actionListener;
            this._list = new List<DownloadedElevationData>();
        }

        public void SetItems(IEnumerable<DownloadedElevationData> items)
        {
            _list = items.ToList();
            MainThread.BeginInvokeOnMainThread(() => { NotifyDataSetChanged(); });
        }

        public override int Count
        {
            get { return _list.Count; }
        }

        public DownloadedElevationData GetById(long id)
        {
            return _list.SingleOrDefault(p => p.Id == id);
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override DownloadedElevationData this[int index]
        {
            get { return _list[index]; }
        }

        public int GetPosition(DownloadedElevationData ded)
        {
            return _list.FindIndex(x => x.Id == ded.Id);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            MainThread.BeginInvokeOnMainThread(() => { NotifyDataSetChanged(); });
        }

        public void Add(DownloadedElevationData item)
        {
            _list.Insert(0, item);
            MainThread.BeginInvokeOnMainThread(() => { NotifyDataSetChanged(); });
        }

        public void Update(DownloadedElevationData item)
        {
            var ded = GetById(item.Id);
            if (ded != null)
            {
                ded.Distance = item.Distance;
                ded.SizeInBytes = item.SizeInBytes;
                MainThread.BeginInvokeOnMainThread(() => { NotifyDataSetChanged(); });
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
            {
                view = _context.LayoutInflater.Inflate(Resource.Layout.DownloadedElevationDataItem, parent, false);
            }

            view.SetOnClickListener(this);
            view.Tag = position;

            DownloadedElevationData item = this[position];
            view.FindViewById<TextView>(Resource.Id.textViewPlaceName).Text = $"{item.PlaceName}";
            view.FindViewById<TextView>(Resource.Id.textViewGpsLocation).Text = GpsUtils.LocationAsString(item.Longitude, item.Latitude);
            var sizeInMBytes = item.SizeInBytes / 1024d / 1024d;
            view.FindViewById<TextView>(Resource.Id.textViewSize).Text = $"{item.Distance} km / {sizeInMBytes:F1} MBytes";

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.PoiDeleteButton);
            deleteButton.Tag = position;
            deleteButton.SetOnClickListener(this);

            return view;
        }

        public void OnClick(View v)
        {
            if (_actionListener == null)
                return;

            int position = (int)v.Tag;

            switch (v.Id)
            {
                case Resource.Id.PoiDeleteButton:
                    _actionListener.OnDedDeleteRequest(position);
                    break;
                case Resource.Id.linearLayoutItem:
                    _actionListener.OnDedEditRequest(position);
                    break;

            }
        }
    }
}
