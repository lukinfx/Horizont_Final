using System;
using System.Collections.Generic;
using System.Text;
using Peaks360Lib.Domain.Models;

namespace Peaks360Lib.Domain.ViewModel
{
    public class DownloadViewItem
    {
        public DownloadViewItem(PoisToDownload fromDatabase, PoiData fromInternet)
        {
            this.fromDatabase = fromDatabase;
            this.fromInternet = fromInternet;
        }

        public PoisToDownload fromDatabase;
        public PoiData fromInternet;
    }
}
