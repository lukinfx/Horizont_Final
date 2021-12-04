using System;
using System.Collections.Generic;
using System.Linq;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class PoiCategoryHelper
    {
        public static List<PoiCategory> GetAllCategories()
        {
            return new PoiCategory[]
            {
                PoiCategory.Mountains, 
                PoiCategory.Cities, 
                PoiCategory.Historic, 
                PoiCategory.Churches, 
                PoiCategory.Lakes, 
                PoiCategory.Transmitters, 
                PoiCategory.ViewTowers, 
                PoiCategory.Other
            }.ToList();
        }

        public static string GetCategoryName(Android.Content.Res.Resources res, PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Historic:
                    return res.GetText(Resource.String.Category_Historic);
                case PoiCategory.Cities:
                    return res.GetText(Resource.String.Category_Cities);
                case PoiCategory.Other:
                    return res.GetText(Resource.String.Category_Other);
                case PoiCategory.Mountains:
                    return res.GetText(Resource.String.Category_Mountains);
                case PoiCategory.Lakes:
                    return res.GetText(Resource.String.Category_Lakes);
                case PoiCategory.ViewTowers:
                    return res.GetText(Resource.String.Category_ViewTowers);
                case PoiCategory.Transmitters:
                    return res.GetText(Resource.String.Category_Transmitters);
                case PoiCategory.Churches:
                    return res.GetText(Resource.String.Category_Churches);
                case PoiCategory.ElevationData:
                    return res.GetText(Resource.String.Category_ElevationData);
                default:
                    return "Unknown category";
            }
        }

        public static PoiCategory GetCategory(int buttonResourceId)
        {
            switch (buttonResourceId)
            {
                case Resource.Id.imageButtonSelectHistoric:
                    return PoiCategory.Historic;
                case Resource.Id.imageButtonSelectCity:
                    return PoiCategory.Cities;
                case Resource.Id.imageButtonSelectOther:
                    return PoiCategory.Other;
                case Resource.Id.imageButtonSelectMountain:
                    return PoiCategory.Mountains;
                case Resource.Id.imageButtonSelectLake:
                    return PoiCategory.Lakes;
                case Resource.Id.imageButtonSelectViewtower:
                    return PoiCategory.ViewTowers;
                case Resource.Id.imageButtonSelectTransmitter:
                    return PoiCategory.Transmitters;
                case Resource.Id.imageButtonSelectChurch:
                    return PoiCategory.Churches;
                default:
                    throw new SystemException("Unsupported category");
            }
        }

        /// <summary>
        /// Returns int representing Icon of the category.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static int GetImage(PoiCategory category, bool enabled = true)
        {
            switch (category)
            {
                case PoiCategory.Historic:
                    return enabled ? Resource.Drawable.c_castle : Resource.Drawable.c_castle_grey;
                case PoiCategory.Cities:
                    return enabled ? Resource.Drawable.c_city : Resource.Drawable.c_city_grey;
                case PoiCategory.Other:
                    return enabled ? Resource.Drawable.c_other : Resource.Drawable.c_other_grey;
                case PoiCategory.Mountains:
                    return enabled ? Resource.Drawable.i_elevation : Resource.Drawable.i_elevation;
                case PoiCategory.Lakes:
                    return enabled ? Resource.Drawable.c_lake : Resource.Drawable.c_lake_grey;
                case PoiCategory.ViewTowers:
                    return enabled ? Resource.Drawable.c_viewtower : Resource.Drawable.c_viewtower_grey;
                case PoiCategory.Transmitters:
                    return enabled ? Resource.Drawable.c_transmitter : Resource.Drawable.c_transmitter_grey;
                case PoiCategory.Churches:
                    return enabled ? Resource.Drawable.c_church : Resource.Drawable.c_church_grey;
                default:
                    return Resource.Drawable.c_basic;
            }
        }

        public static int GetImageInCircle(PoiCategory category, bool enabled = true)
        {
            switch (category)
            {
                case PoiCategory.Historic:
                    return enabled ? Resource.Drawable.c_castle : Resource.Drawable.c_castle_grey;
                case PoiCategory.Cities:
                    return enabled ? Resource.Drawable.c_city : Resource.Drawable.c_city_grey;
                case PoiCategory.Other:
                    return enabled ? Resource.Drawable.c_other : Resource.Drawable.c_other_grey;
                case PoiCategory.Mountains:
                    return enabled ? Resource.Drawable.c_mountain: Resource.Drawable.c_mountain;
                case PoiCategory.Lakes:
                    return enabled ? Resource.Drawable.c_lake : Resource.Drawable.c_lake_grey;
                case PoiCategory.ViewTowers:
                    return enabled ? Resource.Drawable.c_viewtower : Resource.Drawable.c_viewtower_grey;
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