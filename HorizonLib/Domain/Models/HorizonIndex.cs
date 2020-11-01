using System;
using SQLite;
using HorizontLib.Domain.Enums;
using System.Collections.Generic;

namespace HorizontLib.Domain.Models
{

    public class PoiData
    {
        public Guid Id { get; set; }

        public string Description { get; set; }
        public string Url { get; set; }

        public PoiCategory Category { get; set; }
    }

    public class ElevationMapTile
    {
        public int Longitude { get; set; }
        public int Latitude { get; set; }
    }

    public class ElevationMap
    {
        public Guid Id { get; set; }
        public List<ElevationMapTile> ElevationMapTile;
    }

    public class CountryData
    {

        public PoiCountry Country { get; set; }

        public List<PoiData> PoiData;
        public ElevationMap ElevationMap;
    }

    public class HorizonIndex : List<CountryData>
    {
    }
}
