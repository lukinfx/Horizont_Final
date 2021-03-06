﻿using System;
using SQLite;
using Peaks360Lib.Domain.Enums;

namespace Peaks360Lib.Domain.Models
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
        public int PointCount { get; set; }
        public DateTime DateCreated { get; set; }
    }
}