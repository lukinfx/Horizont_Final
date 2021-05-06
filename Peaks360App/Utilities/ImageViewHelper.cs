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
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Utilities
{
    public class ImageViewHelper
    {
        private static ColorMatrixColorFilter _grayscaleFilter;
        private static ColorMatrixColorFilter _highlightedFilter;
        private static ColorMatrixColorFilter _unimportantFilter;

        private static Paint _unimportantPaint;

        public static ColorMatrixColorFilter GetColorFilter(bool disabled, bool highlighted)
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

        private  static ColorMatrixColorFilter GetUnimportantColorFilter()
        {
            if (_unimportantFilter == null)
            {
                var cm = new ColorMatrix();
                //cm.SetRotate(2, -30f); // do zelena
                //cm.SetRotate(2, 5f);//trochu zluta
                //cm.SetRotate(2, 10f);//zluta
                //cm.SetRotate(2, 15f);//oranzova
                //cm.SetRotate(2, 30f); // cervena

                cm.SetSaturation(0.3f);

                /*float contrast = 1f; //0..10 1 is default
                float brightness = -100f;//-255..255 0 is default
                ColorMatrix cm = new ColorMatrix(new float[]
                {
                    contrast, 0, 0, 0, brightness,
                    0, contrast, 0, 0, brightness,
                    0, 0, contrast, 0, brightness,
                    0, 0, 0, 1, 0
                });*/
                _unimportantFilter = new ColorMatrixColorFilter(cm);
            }

            return _unimportantFilter;
        }

        public static Paint GetImportancePaint(Poi poi)
        {
            if (_unimportantPaint == null)
            {
                _unimportantPaint = new Paint();
                _unimportantPaint.SetColorFilter(GetUnimportantColorFilter());
            }

            if (string.IsNullOrEmpty(poi.Wikidata) && string.IsNullOrEmpty(poi.Wikidata))
                return _unimportantPaint;

            return null;
        }

        public static ColorMatrixColorFilter GetImportanceColorFilter(Poi poi)
        {
            if (string.IsNullOrEmpty(poi.Wikidata) && string.IsNullOrEmpty(poi.Wikidata))
                return GetUnimportantColorFilter(); 
            
            return null;
        }

    }
}