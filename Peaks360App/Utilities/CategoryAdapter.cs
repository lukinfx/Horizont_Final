using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class CategoryAdapter : BaseAdapter<PoiCategory?>
    {
        Activity context;
        List<PoiCategory?> list;

        public CategoryAdapter(Activity _context, bool includeAll = false)
            : base()
        {
            this.context = _context;
            list = PoiCategoryHelper.GetAllCategories().Select(x => (PoiCategory?)x).ToList();
            if (includeAll)
            {
                list.Insert(0, null);
            }
        }

        public override int Count
        {
            get { return list.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public int GetPosition(PoiCategory category)
        {
            return list.IndexOf(category);
        }

        public override PoiCategory? this[int index]
        {
            get { return list[index]; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.CategoryItem, parent, false);

            PoiCategory? item = this[position];
            if (item.HasValue)
            {
                view.FindViewById<TextView>(Resource.Id.PoiItemCategoryAsText).Text = PoiCategoryHelper.GetCategoryName(context.Resources, item.Value);
                view.FindViewById<ImageView>(Resource.Id.PoiItemCategoryAsIcon).SetImageResource(PoiCategoryHelper.GetImage(item.Value));
            }
            else
            {
                view.FindViewById<TextView>(Resource.Id.PoiItemCategoryAsText).Text = context.Resources.GetText(Resource.String.Common_AllCategories);
                view.FindViewById<ImageView>(Resource.Id.PoiItemCategoryAsIcon).SetImageResource(Resource.Drawable.c_basic);
            }

            return view;
        }
    }
}


