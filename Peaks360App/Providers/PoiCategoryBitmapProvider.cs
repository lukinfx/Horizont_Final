using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Peaks360Lib.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;

namespace Peaks360App.Providers
{
    public interface IPoiCategoryBitmapProvider
    {
        void Initialize(Resources resources, Size size);
        Bitmap GetCategoryIcon(PoiCategory category);
    }

    public class PoiCategoryBitmapProvider : IPoiCategoryBitmapProvider
    {
        protected Dictionary<PoiCategory, Bitmap> categoryIcon = new Dictionary<PoiCategory, Bitmap>();
        protected Bitmap defaultIcon;

        public PoiCategoryBitmapProvider()
        {
            defaultIcon = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888);
        }

        public virtual void Initialize(Resources resources, Size size)
        {
            lock (categoryIcon)
            {
                categoryIcon.Clear();
                categoryIcon.Add(PoiCategory.Cities, GetCategoryBitmap(resources, PoiCategory.Cities, size));
                categoryIcon.Add(PoiCategory.Mountains, GetCategoryBitmap(resources, PoiCategory.Mountains, size));
                categoryIcon.Add(PoiCategory.Castles, GetCategoryBitmap(resources, PoiCategory.Castles, size));
                categoryIcon.Add(PoiCategory.Churches, GetCategoryBitmap(resources, PoiCategory.Churches, size));
                categoryIcon.Add(PoiCategory.Historic, GetCategoryBitmap(resources, PoiCategory.Historic, size));
                categoryIcon.Add(PoiCategory.Lakes, GetCategoryBitmap(resources, PoiCategory.Lakes, size));
                categoryIcon.Add(PoiCategory.Transmitters, GetCategoryBitmap(resources, PoiCategory.Transmitters, size));
                categoryIcon.Add(PoiCategory.ViewTowers, GetCategoryBitmap(resources, PoiCategory.ViewTowers, size));
                categoryIcon.Add(PoiCategory.Other, GetCategoryBitmap(resources, PoiCategory.Other, size));
            }
        }

        public Bitmap GetCategoryIcon(PoiCategory category)
        {
            if (!categoryIcon.ContainsKey(category))
                return defaultIcon;

            return categoryIcon[category];
        }

        private Bitmap GetCategoryBitmap(Resources resources, PoiCategory category, Size size)
        {
            var resourceId = Utilities.PoiCategoryHelper.GetImage(category);
            var img = BitmapFactory.DecodeResource(resources, resourceId);
            return Bitmap.CreateScaledBitmap(img, size.Width, size.Height, false);
        }

    }
}