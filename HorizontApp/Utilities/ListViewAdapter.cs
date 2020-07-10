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
using HorizontApp.Domain.Enums;
using HorizontApp.Domain.ViewModel;

namespace HorizontApp.Utilities
{
	public class ListViewAdapter : BaseAdapter<PoiViewItem>
	{
		Activity context;
		List<PoiViewItem> list;
		private ImageView Thumbnail;
		TextView Favourite;

		public ListViewAdapter(Activity _context,IEnumerable<PoiViewItem> _list)
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

		public override PoiViewItem this[int index]
		{
			get { return list[index]; }
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			View view = convertView;

			// re-use an existing view, if one is available
			// otherwise create a new one
			if (view == null)
				view = context.LayoutInflater.Inflate(Resource.Layout.ListItemLayout, parent, false);

			PoiViewItem item = this[position];
			view.FindViewById<TextView>(Resource.Id.Title).Text = item.Name;
			view.FindViewById<TextView>(Resource.Id.Description).Text = Convert.ToString(item.Altitude) + "m | " + Convert.ToString(Math.Round(item.Distance/1000, 2)) + " km";

			Favourite = view.FindViewById<TextView>(Resource.Id.Favourite);
			if (item.Favorite)
				Favourite.Text = "❤";
			else 
				Favourite.Text = "♡";

			Thumbnail = view.FindViewById<ImageView>(Resource.Id.Thumbnail);
			Thumbnail.SetImageResource(GetImage(item));

			//using (var imageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail))
			//{
			//	string url = Android.Text.Html.FromHtml(item.thumbnail).ToString();

			//	Koush.UrlImageViewHelper.SetUrlDrawable(imageView,
			//		url, Resource.Drawable.Placeholder);
			//}
			return view;
		}
		public int GetImage(PoiViewItem item)
        {
			switch (item.Category)
			{
				case PoiCategory.Castles:
					return Resource.Drawable.c_castle;
				case PoiCategory.Mountains:
					return Resource.Drawable.c_mountain;
				case PoiCategory.Lakes:
					return Resource.Drawable.c_lake;
				case PoiCategory.ViewTowers:
					return Resource.Drawable.c_viewtower;
				case PoiCategory.Palaces:
					return Resource.Drawable.c_palace;
				case PoiCategory.Ruins:
					return Resource.Drawable.c_ruins;
				case PoiCategory.Transmitters:
					return Resource.Drawable.c_transmitter;
				default:
					return Resource.Drawable.c_basic;
			}
		}
		public static void RefreshFavourite()
        {
			
		}
	}
}