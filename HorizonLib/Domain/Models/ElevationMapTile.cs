using System;
using System.Collections.Generic;
using System.Text;

namespace HorizonLib.Domain.Models
{
    public class ElevationMapTile
    {
        public int Longitude { get; set; }
        public int Latitude { get; set; }
    }

    public class ElevationMap
    {
        public List<ElevationMapTile> Tiles;
    }
}
