using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Graphics;

namespace Peaks360App.Utilities
{
    public class ImageViewHelper
    {
        private static ColorMatrixColorFilter _grayscaleFilter;

        public static void ShowAsEnabled(ImageView imageView, bool enabled)
        {
            if (_grayscaleFilter == null)
            {
                var cm = new ColorMatrix();
                cm.SetSaturation(0.0f);
                _grayscaleFilter = new ColorMatrixColorFilter(cm);
            }

            imageView.SetColorFilter(enabled ? null : _grayscaleFilter);
        }
    }
}