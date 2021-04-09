using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Utilities
{
    public class DownloadItemAdapter : BaseAdapter<DownloadViewItem>
    {
        private Activity context;
        private List<DownloadViewItem> list;

        public DownloadItemAdapter(Activity _context)
            : base()
        {
            this.context = _context;
            this.list = new List<DownloadViewItem>();
        }

        public void SetItems(IEnumerable<DownloadViewItem> items)
        {
            list = items.ToList();
            NotifyDataSetChanged();
        }

        public override int Count
        {
            get { return list.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override DownloadViewItem this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.DownloadItemListLayout, parent, false);

            DownloadViewItem item = this[position];
            view.FindViewById<TextView>(Resource.Id.PoiItemCategoryAsText).Text = PoiCategoryHelper.GetCategoryName(context.Resources, item.fromDatabase.Category);

            string downloadDate;
            bool updateAvailable = false;
            if (item.fromDatabase.DownloadDate.HasValue)
            {
                downloadDate = context.Resources.GetText(Resource.String.Download_DownloadedOn) + " " + item.fromDatabase.DownloadDate.Value.ToString("yyyy-MM-dd");
                if (item.fromInternet.DateCreated > item.fromDatabase.DateCreated)
                {
                    updateAvailable = true;
                }
            }
            else
            {
                downloadDate = context.Resources.GetText(Resource.String.Download_NotDownloadedYet);
            }
            view.FindViewById<TextView>(Resource.Id.PoiItemDownloadedDate).Text = downloadDate;
            view.FindViewById<TextView>(Resource.Id.PoiItemDateCreated).Text = updateAvailable ? "Update is available" : "";
            view.FindViewById<TextView>(Resource.Id.PoiItemDateCreated).SetTextColor(updateAvailable ? Color.DarkRed : Color.Black);
            //view.FindViewById<TextView>(Resource.Id.PoiItemDateCreated).Text = $"Created on {item.fromDatabase.DateCreated.ToString("yyyy-MM-dd")} (Count:{item.fromDatabase.PointCount})";

            var image = view.FindViewById<ImageView>(Resource.Id.PoiItemCategoryAsIcon);
            ImageViewHelper.ShowAsEnabled(image, item.fromDatabase.DownloadDate != null);
            image.SetImageResource(PoiCategoryHelper.GetImage(item.fromDatabase.Category));

            return view;
        }
    }
}
