using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class DownloadCountryAdapter : BaseAdapter<PoiCountry>
    {
        private LayoutInflater _layoutInflater;
        private List<PoiCountry> list;
        private bool _highlightSelection;
        private Android.Graphics.Color highlightedColor = new Android.Graphics.Color(0, 0, 0, 32);
        private Android.Graphics.Color normalColor = new Android.Graphics.Color(0, 0, 0, 0);


        private PoiCountry? _selection;
        public PoiCountry? Selection
        {
            set { _selection = value; NotifyDataSetChanged(); } private get { return _selection; }
        }
         
        //public PoiCountry? Selection { set; private get; }

        public DownloadCountryAdapter(LayoutInflater layoutInflater, bool highlightSelection)
            : base()
        {
            this._layoutInflater = layoutInflater;
            list = new List<PoiCountry>();
            _highlightSelection = highlightSelection;
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
                view = _layoutInflater.Inflate(Resource.Layout.DownloadCountryListLayout, parent, false);

            PoiCountry item = this[position];
            view.FindViewById<TextView>(Resource.Id.PoiItemCountryAsText).Text = PoiCountryHelper.GetCountryName(item);
            view.FindViewById<ImageView>(Resource.Id.PoiItemCountryAsIcon).SetImageResource(PoiCountryHelper.GetCountryIcon(item));

            if (_highlightSelection)
            {
                view.SetBackgroundColor(item == Selection ? highlightedColor : normalColor);
            }

            return view;
        }
    }
}