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
            view.FindViewById<ImageView>(Resource.Id.PoiItemCountryAsIcon).SetImageResource(GetCountryIcon(item));

            return view;
        }

        private int GetCountryIcon(PoiCountry country)
        {
            //Icons from https://www.countryflags.com/en/icons-overview/
            switch (country)
            {
                case PoiCountry.AUT:
                    return Resource.Drawable.flag_of_Austria;
                case PoiCountry.CZE:
                    return Resource.Drawable.flag_of_CzechRepublic;
                case PoiCountry.FRA:
                    return Resource.Drawable.flag_of_France;
                case PoiCountry.DEU:
                    return Resource.Drawable.flag_of_Georgia;
                case PoiCountry.HUN:
                    return Resource.Drawable.flag_of_Hungary;
                case PoiCountry.ITA:
                    return Resource.Drawable.flag_of_Italy;
                case PoiCountry.POL:
                    return Resource.Drawable.flag_of_Poland;
                case PoiCountry.ROU:
                    return Resource.Drawable.flag_of_Romania;
                case PoiCountry.SVK:
                    return Resource.Drawable.flag_of_Slovakia;
                case PoiCountry.SVN:
                    return Resource.Drawable.flag_of_Slovenia;
                case PoiCountry.ESP:
                    return Resource.Drawable.flag_of_Spain;
                case PoiCountry.CHE:
                    return Resource.Drawable.flag_of_Switzerland;
                case PoiCountry.BEL:
                    return Resource.Drawable.flag_of_Belgium;
                case PoiCountry.BIH:
                    return Resource.Drawable.flag_of_BosniaHerzegovina;
                case PoiCountry.HRV:
                    return Resource.Drawable.flag_of_Croatia;
                case PoiCountry.BGR:
                    return Resource.Drawable.flag_of_Bulgaria;
                case PoiCountry.DNK:
                    return Resource.Drawable.flag_of_Denmark;
                case PoiCountry.FIN:
                    return Resource.Drawable.flag_of_Finland;
                case PoiCountry.LIE:
                    return Resource.Drawable.flag_of_Liechtenstein;
                case PoiCountry.LUX:
                    return Resource.Drawable.flag_of_Luxembourg;
                case PoiCountry.NLD:
                    return Resource.Drawable.flag_of_Netherlands;
                case PoiCountry.NOR:
                    return Resource.Drawable.flag_of_Norway;
                case PoiCountry.ERB:
                    return Resource.Drawable.flag_of_Serbia;
                case PoiCountry.SWE:
                    return Resource.Drawable.flag_of_Sweden;
                case PoiCountry.GRC:
                    return Resource.Drawable.flag_of_Greece;
                case PoiCountry.UKR:
                    return Resource.Drawable.flag_of_Ukraine;
                case PoiCountry.BLR:
                    return Resource.Drawable.flag_of_Belarus;
                case PoiCountry.ALB:
                    return Resource.Drawable.flag_of_Albania;
                case PoiCountry.CYP:
                    return Resource.Drawable.flag_of_Cyprus;
                case PoiCountry.EST:
                    return Resource.Drawable.flag_of_Estonia;
                case PoiCountry.GBR:
                    return Resource.Drawable.flag_of_UnitedKingdom;
                case PoiCountry.XKX:
                    return Resource.Drawable.flag_of_Kosovo;
                case PoiCountry.LVA:
                    return Resource.Drawable.flag_of_Latvia;
                case PoiCountry.LTU:
                    return Resource.Drawable.flag_of_Lithuania;
                case PoiCountry.MLT:
                    return Resource.Drawable.flag_of_Malta;
                case PoiCountry.MCO:
                    return Resource.Drawable.flag_of_Monaco;
                case PoiCountry.PRT:
                    return Resource.Drawable.flag_of_Portugal;
                case PoiCountry.RUS:
                    return Resource.Drawable.flag_of_Russia;
                case PoiCountry.AND:
                    return Resource.Drawable.flag_of_Andorra;
                case PoiCountry.FRO:
                    return Resource.Drawable.flag_of_Austria;
                case PoiCountry.GEO:
                    return Resource.Drawable.flag_of_Georgia;
                case PoiCountry.MNE:
                    return Resource.Drawable.flag_of_Montenegro;
                case PoiCountry.MDA:
                    return Resource.Drawable.flag_of_Moldova;
                case PoiCountry.MKD:
                case PoiCountry.IRL:
                case PoiCountry.GGY:
                case PoiCountry.IMN:
                case PoiCountry.AZO:
                default:
                    return Resource.Drawable.flag_of_Unknown;
            }

        }
    }
}