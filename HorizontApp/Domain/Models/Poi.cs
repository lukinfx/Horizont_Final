﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Enums;

namespace HorizontApp.Domain.Models
{
    public class Poi
    {
        public Guid Id;
        public PoiCategory Category;
        public string Name;

        public GpsLocation GpsLocation;
        public bool Favorite;
    }
}