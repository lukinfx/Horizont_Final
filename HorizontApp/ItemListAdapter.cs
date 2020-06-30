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

namespace HorizontApp
{
    public class Post
    {
        public string url { get; set; }
        public string title { get; set; }
        public string description { get; set; }
    }

    public class ItemListAdapter : BaseAdapter<Post>
    {
        Activity context;
        List<Post> list;

        public ItemListAdapter(Activity _context, List<Post> _list)
            : base()
        {
            this.context = _context;
            this.list = _list;
        }

        public override int Count
        {
            get { return list.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Post this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            // re-use an existing view, if one is available
            // otherwise create a new one
            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.ItemListRow, parent, false);

            Post item = this[position];
            view.FindViewById<TextView>(Resource.Id.Title).Text = item.title;
            view.FindViewById<TextView>(Resource.Id.Description).Text = item.description;

            using (var imageView = view.FindViewById<ImageView>(Resource.Id.Thumbnail))
            {
               // string url = Android.Text.Html.FromHtml(item.url).ToString();

                //Download and display image
                //Koush.UrlImageViewHelper.SetUrlDrawable(imageView, url, Resource.Drawable.Placeholder);
            }
            return view;
        }
    }
}
