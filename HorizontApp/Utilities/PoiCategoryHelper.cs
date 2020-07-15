using HorizontApp.Domain.Enums;

namespace HorizontApp.Utilities
{
    public class PoiCategoryHelper
    {
        public static int GetImage(PoiCategory category)
        {
            switch (category)
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
                case PoiCategory.Churches:
                    return Resource.Drawable.c_church;
                default:
                    return Resource.Drawable.c_basic;
            }
        }
    }
}