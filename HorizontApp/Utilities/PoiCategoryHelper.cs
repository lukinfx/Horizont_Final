using HorizontApp.Domain.Enums;

namespace HorizontApp.Utilities
{
    public class PoiCategoryHelper
    {
        public static int GetImage(PoiCategory category, bool enabled = true)
        {
            switch (category)
            {
                case PoiCategory.Castles:
                    return enabled ? Resource.Drawable.c_castle : Resource.Drawable.c_castle_grey;
                case PoiCategory.Mountains:
                    return enabled ? Resource.Drawable.c_mountain : Resource.Drawable.c_mountain_grey;
                case PoiCategory.Lakes:
                    return enabled ? Resource.Drawable.c_lake : Resource.Drawable.c_lake_grey;
                case PoiCategory.ViewTowers:
                    return enabled ? Resource.Drawable.c_viewtower : Resource.Drawable.c_viewtower_grey;
                case PoiCategory.Palaces:
                    return enabled ? Resource.Drawable.c_palace : Resource.Drawable.c_palace_grey;
                case PoiCategory.Ruins:
                    return enabled ? Resource.Drawable.c_ruins : Resource.Drawable.c_ruins_grey;
                case PoiCategory.Transmitters:
                    return enabled ? Resource.Drawable.c_transmitter : Resource.Drawable.c_transmitter_grey;
                case PoiCategory.Churches:
                    return enabled ? Resource.Drawable.c_church : Resource.Drawable.c_church_grey;
                default:
                    return Resource.Drawable.c_basic;
            }
        }
    }
}