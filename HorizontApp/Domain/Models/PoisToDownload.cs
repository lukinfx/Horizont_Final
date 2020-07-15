using System;
using HorizontApp.Domain.Enums;
using SQLite;

namespace HorizontApp.Domain.Models
{
    public class PoisToDownload
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        public string Description { get; set; }
        public string Url { get; set; }
        public PoiCountry Country { get; set; }
        public PoiCategory Category { get; set; }

        public DateTime? DownloadDate { get; set; }
    }
}