using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontApp.Domain.ViewModel;

namespace HorizontApp.Utilities
{
    public class DownloadCountryAdapter : BaseAdapter<PoiCountry>
    {
        Activity context;
        List<PoiCountry> list;

        public DownloadCountryAdapter(Activity _context, IEnumerable<PoiCountry> _list)
            : base()
        {
            this.context = _context;
            this.list = _list.ToList();
        }

        public override int Count
        {
            get { return list.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
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
            view.FindViewById<TextView>(Resource.Id.PoiItemCountryAsText).Text = GetCountryName(item);
            view.FindViewById<ImageView>(Resource.Id.PoiItemCountryAsIcon).SetImageResource(GetCountryIcon(item));

            return view;
        }

        private string GetCountryName(PoiCountry country)
        {
            switch (country)
            {
                case PoiCountry.AUT:
                    return "Austria";
                case PoiCountry.CZE:
                    return "Czech republic";
                case PoiCountry.FRA:
                    return "France";
                case PoiCountry.DEU:
                    return "Germany";
                case PoiCountry.HUN:
                    return "Hungary";
                case PoiCountry.ITA:
                    return "Italy";
                case PoiCountry.POL:
                    return "Poland";
                case PoiCountry.ROU:
                    return "Romania";
                case PoiCountry.SVK:
                    return "Slovakia";
                case PoiCountry.SVN:
                    return "Slovenia";
                case PoiCountry.ESP:
                    return "Spain";
                case PoiCountry.CHE:
                    return "Switzerland";
                default:
                    return "Unknown";
            }
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