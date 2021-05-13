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
        private static ColorMatrixColorFilter _cfPoiFavourite;
        private static Paint _pPoiCommon;
        private static Paint _pPoiImportant;
        private static Paint _pPoiFavourite;

        private static ColorMatrixColorFilter GetImportantColorFilter()
        {
            if (_cfPoiImportant == null)
            {
                var cm = new ColorMatrixBuilder().Create();
                _cfPoiImportant = new ColorMatrixColorFilter(cm);
            }

            return _cfPoiImportant;
        }
        
        private static ColorMatrixColorFilter GetFavouriteColorFilter()
        {
            if (_cfPoiFavourite == null)
            {
                var cm = new ColorMatrixBuilder().Hue(-30).Create();
                _cfPoiFavourite = new ColorMatrixColorFilter(cm);
            }

            return _cfPoiFavourite;
        }

        private static ColorMatrixColorFilter GetCommonColorFilter()
        {
            if (_cfPoiCommon == null)
            {
                var cm = new ColorMatrixBuilder().Hue(+30).Brightness(-100).Saturation(0.4f).Create();
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

                _pPoiFavourite = new Paint();
                _pPoiFavourite.SetColorFilter(GetFavouriteColorFilter());
            }

            if (item.Poi.Favorite)
            {
                return _pPoiFavourite;
            }
            return item.IsImportant() ? _pPoiImportant : _pPoiCommon;
        }

        public static ColorMatrixColorFilter GetColorFilter(PoiViewItem item)
        {
            if (item.Poi.Favorite)
            {
                return GetFavouriteColorFilter();
            }

            return item.IsImportant() ? GetImportantColorFilter() : GetCommonColorFilter();
        }

    }
}