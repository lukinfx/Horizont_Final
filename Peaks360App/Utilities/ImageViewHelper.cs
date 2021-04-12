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
        private static ColorMatrixColorFilter _highlightedFilter;

        public static ColorMatrixColorFilter GetColorFilter(ImageView imageView, bool disabled, bool highlighted)
        {
            if (_grayscaleFilter == null)
            {
                var cm = new ColorMatrix();
                cm.SetSaturation(0.0f);
                _grayscaleFilter = new ColorMatrixColorFilter(cm);
            }

            if (_highlightedFilter == null)
            {
                // Rotation - Red axis = 0, green axis = 1, blue axis = 2
                var cm = new ColorMatrix();
                //cm.SetRotate(2, -30f); // do zelena
                //cm.SetRotate(2, 5f);//trochu zluta
                //cm.SetRotate(2, 10f);//zluta
                cm.SetRotate(2, 11f);
                //cm.SetRotate(2, 15f);//oranzova
                //cm.SetRotate(2, 30f); // cervena


                /*float contrast = 1f; //0..10 1 is default
                float brightness = 0f;//-255..255 0 is default
                ColorMatrix cm = new ColorMatrix(new float[]
                {
                    contrast, 0, 0, 0, brightness,
                    0, contrast, 0, 0, brightness,
                    0, 0, contrast, -5, brightness,
                    0, 0, 0, 1, 0
                });*/

                _highlightedFilter = new ColorMatrixColorFilter(cm);
            }

            if (disabled)
                return _grayscaleFilter;

            if (highlighted)
                return _highlightedFilter;

            return null;
        }

    }
}