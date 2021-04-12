using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class DownloadCountryAdapter : BaseAdapter<PoiCountry>
    {
        Activity context;
        List<PoiCountry> list;

        public DownloadCountryAdapter(Activity _context)
            : base()
        {
            this.context = _context;
            list = new List<PoiCountry>();
        }

        public void SetItems(IEnumerable<PoiCountry> items)
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

        public int GetPosition(PoiCountry country)
        {
            return list.IndexOf(country);
        }

        public override PoiCountry this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.DownloadCountryListLayout, parent, false);

            PoiCountry item = this[position];
            view.FindViewById<TextView>(Resource.Id.PoiItemCountryAsText).Text = PoiCountryHelper.GetCountryName(item);
            view.FindViewById<ImageView>(Resource.Id.PoiItemCountryAsIcon).SetImageResource(PoiCountryHelper.GetCountryIcon(item));

            return view;
        }
    }
}