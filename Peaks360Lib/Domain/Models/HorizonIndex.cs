using System;
using SQLite;
using Peaks360Lib.Domain.Enums;
using System.Collections.Generic;

namespace Peaks360Lib.Domain.Models
{

    public class PoiData
    {
        public Guid Id { get; set; }

        public string Description { get; set; }
        public string Url { get; set; }
        public int PointCount { get; set; }
        public DateTime DateCreated { get; set; }

        public PoiCategory Category { get; set; }
    }

    public class CountryData
    {
        public PoiCountry Country { get; set; }
        public List<PoiData> PoiData;
    }

    public class HorizonIndex : List<CountryData>
    {
    }
}
