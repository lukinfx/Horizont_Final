using System;
using HorizontApp.Domain.Enums;

namespace HorizontApp.Domain.Models
{
    public class PoisToDownload
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public PoiCountry Country { get; set; }
        public PoiCategory Category { get; set; }
    }
}