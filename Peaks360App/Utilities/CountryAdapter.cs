using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class CountryAdapter : BaseAdapter<PoiCountry?>
    {
        Activity context;
        List<PoiCountry?> list;

        public CountryAdapter(Activity _context)
            : base()
        {
            this.context = _context;
            list = PoiCountryHelper.GetAllCountries().Select(x => (PoiCountry?)x)
                .OrderBy(x => PoiCountryHelper.GetCountryName(x)).ToList();
            list.Insert(0, null);
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

        public override PoiCountry? this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.CountryItem, parent, false);

            PoiCountry? item = this[position];
            if (item.HasValue)
            {
                view.FindViewById<TextView>(Resource.Id.PoiItemCountryAsText).Text = PoiCountryHelper.GetCountryName(item);
                view.FindViewById<ImageView>(Resource.Id.PoiItemCountryAsIcon).SetImageResource(PoiCountryHelper.GetCountryIcon(item.Value));
            }
            else
            {
                view.FindViewById<TextView>(Resource.Id.PoiItemCountryAsText).Text = context.Resources.GetText(Resource.String.Common_AllCountries);
                view.FindViewById<ImageView>(Resource.Id.PoiItemCountryAsIcon).SetImageResource(Resource.Drawable.flag_of_Unknown);
            }

            return view;
        }
    }
}


