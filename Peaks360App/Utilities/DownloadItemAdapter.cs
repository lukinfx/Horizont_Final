using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Utilities
{
    public class DownloadItemAdapter : BaseAdapter<PoisToDownload>
    {
        private Activity context;
        private List<PoisToDownload> list;

        public DownloadItemAdapter(Activity _context)
            : base()
        {
            this.context = _context;
            this.list = new List<PoisToDownload>();
        }

        public void SetItems(IEnumerable<PoisToDownload> items)
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

        public override PoisToDownload this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.DownloadItemListLayout, parent, false);

            PoisToDownload item = this[position];
            view.FindViewById<TextView>(Resource.Id.PoiItemCategoryAsText).Text = PoiCategoryHelper.GetCategoryName(context.Resources, item.Category);
            if (item.DownloadDate != null)
                view.FindViewById<TextView>(Resource.Id.PoiItemDownloadedDate).Text = context.Resources.GetText(Resource.String.Download_DownloadedOn) + item.DownloadDate;
            else
                view.FindViewById<TextView>(Resource.Id.PoiItemDownloadedDate).Text = context.Resources.GetText(Resource.String.Download_NotDownloadedYet);

            var image = view.FindViewById<ImageView>(Resource.Id.PoiItemCategoryAsIcon);
            ImageViewHelper.ShowAsEnabled(image, item.DownloadDate != null);
            image.SetImageResource(PoiCategoryHelper.GetImage(item.Category));

            return view;
        }
    }
}
