using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Utilities
{
    public interface IDownloadItemActionListener
    {
        void OnDownloadItemDelete(int position);
        void OnDownloadItemEdit(int position);
        void OnDownloadItemRefresh(int position);
    }


    public class DownloadItemAdapter : BaseAdapter<DownloadViewItem>, View.IOnClickListener
    {
        private Activity _context;
        private List<DownloadViewItem> _list;
        private IDownloadItemActionListener _downloadItemActionListener;

        public DownloadItemAdapter(Activity _context, IDownloadItemActionListener downloadItemActionListener)
            : base()
        {
            this._context = _context;
            this._downloadItemActionListener = downloadItemActionListener;
            this._list = new List<DownloadViewItem>();
        }

        public void SetItems(IEnumerable<DownloadViewItem> items)
        {
            _list = items.ToList();
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

        public override DownloadViewItem this[int index]
        {
            get { return _list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
            {
                view = _context.LayoutInflater.Inflate(Resource.Layout.DownloadItemListLayout, parent, false);
            }

            view.Tag = position;
            view.SetOnClickListener(this);

            DownloadViewItem item = this[position];

            view.FindViewById<TextView>(Resource.Id.PoiItemCategoryAsText).Text = PoiCategoryHelper.GetCategoryName(_context.Resources, item.fromDatabase.Category);

            string downloadDateText;
            string pointCountText;
            bool updateAvailable = false;
            bool alreadyDownloaded = item.fromDatabase.DownloadDate != null;

            if (item.fromDatabase.DownloadDate.HasValue)
            {
                alreadyDownloaded = true;

                if (item.fromInternet.DateCreated > item.fromDatabase.DateCreated)
                {
                    updateAvailable = true;
                }

                downloadDateText = _context.Resources.GetText(Resource.String.Download_DownloadedOn) + ": " + item.fromDatabase.DownloadDate.Value.ToString("yyyy-MM-dd");
                pointCountText = _context.Resources.GetText(Resource.String.Download_PointCount) + ": " + item.fromDatabase.PointCount;
            }
            else
            {
                downloadDateText = _context.Resources.GetText(Resource.String.Download_NotDownloadedYet);
                pointCountText = _context.Resources.GetText(Resource.String.Download_PointCount) + ": " + item.fromInternet.PointCount;
            }

            view.FindViewById<TextView>(Resource.Id.PoiItemDownloadedDate).Text = downloadDateText;
            view.FindViewById<TextView>(Resource.Id.PoiItemDateCreated).Text = pointCountText;

            var image = view.FindViewById<ImageView>(Resource.Id.PoiItemCategoryAsIcon);
            image.SetColorFilter(ImageViewHelper.GetColorFilter(!alreadyDownloaded, updateAvailable));
            image.SetImageResource(PoiCategoryHelper.GetImage(item.fromDatabase.Category));

            var deleteButton = view.FindViewById<ImageButton>(Resource.Id.PoiDeleteButton);
            deleteButton.SetOnClickListener(this);
            deleteButton.Tag = position;
            deleteButton.Enabled = alreadyDownloaded;

            var refreshButton = view.FindViewById<ImageButton>(Resource.Id.PoiRefreshButton);
            refreshButton.SetOnClickListener(this);
            refreshButton.Tag = position;
            refreshButton.Visibility = updateAvailable ? ViewStates.Visible : ViewStates.Gone;

            return view;
        }

        public void OnClick(View v)
        {
            if (_downloadItemActionListener == null)
                return;

            int position = (int)v.Tag;

            switch (v.Id)
            {
                case Resource.Id.PoiDeleteButton:
                    _downloadItemActionListener.OnDownloadItemDelete(position);
                    break;
                case Resource.Id.PoiRefreshButton:
                    _downloadItemActionListener.OnDownloadItemRefresh(position);
                    break; 
                case Resource.Id.linearLayoutItem:
                    _downloadItemActionListener.OnDownloadItemEdit(position);
                    break;
            }
        }
    }
}
