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
using SQLite;

namespace HorizontApp.Domain.Models
{
    public sealed class Poi
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        public PoiCategory Category { get; set; }
        public string Name { get; set; }

        //public GpsLocation GpsLocation { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }

        public bool Favorite { get; set; }

        public Poi()
        {
        }

        public Poi(Poi poi)
        {
            Id = poi.Id;
            Category = poi.Category;
            Name = poi.Name;
            Longitude = poi.Longitude;
            Latitude = poi.Latitude;
            Altitude = poi.Altitude;
            Favorite = poi.Favorite;
        }

    }
}