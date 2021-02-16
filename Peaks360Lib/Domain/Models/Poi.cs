using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peaks360Lib.Domain.Enums;
using SQLite;

namespace Peaks360Lib.Domain.Models
{
    public sealed class Poi
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        public PoiCategory Category { get; set; }
        public string Name { get; set; }
        public string NameNoAccent { get; set; }

        //public GpsLocation GpsLocation { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }

        public bool Favorite { get; set; }
        public Guid Source { get; set; }
        public string Wikidata { get; set; }
        public string Wikipedia { get; set; }

        public Poi()
        {
        }
    }
}