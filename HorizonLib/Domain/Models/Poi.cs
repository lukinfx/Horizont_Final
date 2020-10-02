using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HorizontLib.Domain.Enums;
using SQLite;

namespace HorizontLib.Domain.Models
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
        public Guid Source { get; set; }

        public Poi()
        {
        }
    }
}