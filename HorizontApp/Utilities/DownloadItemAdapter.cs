using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;

namespace HorizontApp.Utilities
{
    public class DownloadItemAdapter : BaseAdapter<PoisToDownload>
    {
        Activity context;
        List<PoisToDownload> list;

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
            view.FindViewById<TextView>(Resource.Id.PoiItemCategoryAsText).Text = item.Category.ToString();
            view.FindViewById<TextView>(Resource.Id.PoiItemDescription).Text = item.Description;
            if (item.DownloadDate != null)
                view.FindViewById<TextView>(Resource.Id.PoiItemDownloadedDate).Text = "Downloaded on " + item.DownloadDate;
            else
                view.FindViewById<TextView>(Resource.Id.PoiItemDownloadedDate).Text = "Not downloaded yet";

            view.FindViewById<ImageView>(Resource.Id.PoiItemCategoryAsIcon).SetImageResource(PoiCategoryHelper.GetImage(item.Category));

            return view;
        }

        private int GetCountryIcon(PoiCountry country)
        {
            //Icons from https://www.countryflags.com/en/icons-overview/
            switch (country)
            {
                case PoiCountry.AUT:
                    return Resource.Drawable.f_austriaFlagIcon128;
                case PoiCountry.CZE:
                    return Resource.Drawable.f_czechFlagIcon128;
                case PoiCountry.FRA:
                    return Resource.Drawable.f_franceFlagIcon128;
                case PoiCountry.DEU:
                    return Resource.Drawable.f_germanyFlagIcon128;
                case PoiCountry.HUN:
                    return Resource.Drawable.f_hungaryFlagIcon128;
                case PoiCountry.ITA:
                    return Resource.Drawable.f_italyFlagIcon128;
                case PoiCountry.POL:
                    return Resource.Drawable.f_polandFlagIcon128;
                case PoiCountry.ROU:
                    return Resource.Drawable.f_romaniaFlagIcon128;
                case PoiCountry.SVK:
                    return Resource.Drawable.f_slovakiaFlagIcon128;
                case PoiCountry.SVN:
                    return Resource.Drawable.f_sloveniaFlagIcon128;
                case PoiCountry.ESP:
                    return Resource.Drawable.f_spainFlagIcon128;
                case PoiCountry.CHE:
                    return Resource.Drawable.f_switzerlandFlagIcon128;
                default:
                    return Resource.Drawable.f_unknownFlagIcon128;
            }

        }
    }
}
