using Android.Graphics;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Utilities
{
    public class ColorFilterDownloadItem
    {
        private static ColorMatrixColorFilter _cdDownloadItemGrayscale;
        private static ColorMatrixColorFilter _cdDownloadItemHighlighted;

        public static ColorMatrixColorFilter GetColorFilter(bool disabled, bool highlighted)
        {
            if (_cdDownloadItemGrayscale == null)
            {
                var cm = new ColorMatrixBuilder().Saturation(0.0f).Create();
                _cdDownloadItemGrayscale = new ColorMatrixColorFilter(cm);
            }

            if (_cdDownloadItemHighlighted == null)
            {
                var cm = new ColorMatrixBuilder().Hue(-20f).Create();
                _cdDownloadItemHighlighted = new ColorMatrixColorFilter(cm);
            }

            if (disabled)
                return _cdDownloadItemGrayscale;

            if (highlighted)
                return _cdDownloadItemHighlighted;

            return null;
        }
    }


    public class ColorFilterPoiItem
    {
        private static ColorMatrixColorFilter _cfPoiCommon;
        private static ColorMatrixColorFilter _cfPoiImportant;
        private static Paint _pPoiCommon;
        private static Paint _pPoiImportant;

        private static ColorMatrixColorFilter GetImportantColorFilter()
        {
            if (_cfPoiImportant == null)
            {
                var cm = new ColorMatrixBuilder().Hue(-30).Create(); //gold
                _cfPoiImportant = new ColorMatrixColorFilter(cm);
            }

            return _cfPoiImportant;
        }

        private static ColorMatrixColorFilter GetCommonColorFilter()
        {
            if (_cfPoiCommon == null)
            {
                //light green var cm = new ColorMatrixBuilder().Contrast(0.9f).Brightness(-50f).Alpha(0.9f).Hue(20f).Saturation(0.5f).Create();
                //gray var cm = new ColorMatrixBuilder().Brightness(-50f).Saturation(0f).Create();
                var cm = new ColorMatrixBuilder().Create();
                _cfPoiCommon = new ColorMatrixColorFilter(cm);
            }

            return _cfPoiCommon;
        }

        public static Paint GetPaintFilter(PoiViewItem item)
        {
            if (_pPoiCommon == null || _pPoiImportant == null)
            {
                _pPoiCommon = new Paint();
                _pPoiCommon.SetColorFilter(GetCommonColorFilter());

                _pPoiImportant = new Paint();
                _pPoiImportant.SetColorFilter(GetImportantColorFilter());

            }

            return item.IsImportant() ? _pPoiImportant : _pPoiCommon;
        }

        public static ColorMatrixColorFilter GetColorFilter(PoiViewItem item)
        {
            return item.IsImportant() ? GetImportantColorFilter() : GetCommonColorFilter();
        }

    }
}