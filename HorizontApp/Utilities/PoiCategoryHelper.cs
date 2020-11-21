﻿using System;
using HorizontLib.Domain.Enums;

namespace HorizontApp.Utilities
{
    public class PoiCategoryHelper
    {
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
                    return enabled ? Resource.Drawable.c_mountain : Resource.Drawable.c_mountain_grey;
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