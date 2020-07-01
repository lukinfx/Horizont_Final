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
using HorizontApp.Domain.ViewModel;

namespace HorizontApp.Utilities
{
	public class ListViewAdapter : BaseAdapter<PoiViewItem>
	{
		Activity context;
		List<PoiViewItem> list;

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
			view.FindViewById<TextView>(Resource.Id.Description).Text = Convert.ToString(item.Altitude);

			//using (var imageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail))
			//{
			//	string url = Android.Text.Html.FromHtml(item.thumbnail).ToString();

			//	Koush.UrlImageViewHelper.SetUrlDrawable(imageView,
			//		url, Resource.Drawable.Placeholder);
			//}
			return view;
		}
	}
}