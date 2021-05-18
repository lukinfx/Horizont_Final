using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;
using Xamarin.Essentials;

namespace Peaks360App.Models
{
    public class DownloadedElevationDataEventArgs : EventArgs { public DownloadedElevationData data; }
    public delegate void DownloadedElevationDataEventHandler(object sender, DownloadedElevationDataEventArgs e);

    public class DownloadedElevationDataModel
    {
        private PoiDatabase _database;

        public event DownloadedElevationDataEventHandler DownloadedElevationDataAdded;
        public event DownloadedElevationDataEventHandler DownloadedElevationDataUpdated;
        public event DownloadedElevationDataEventHandler DownloadedElevationDataDeleted;

        public DownloadedElevationDataModel(PoiDatabase database)
        {
            _database = database;
        }

        public async Task<IEnumerable<DownloadedElevationData>> GetDownloadedElevationData()
        {
            return await _database.GetDownloadedElevationDataAsync();
        }

        public void InsertItem(DownloadedElevationData item)
        {
            _database.InsertItem(item);
            MainThread.BeginInvokeOnMainThread(() =>
                DownloadedElevationDataAdded?.Invoke(this, new DownloadedElevationDataEventArgs() {data = item})
            );
        }

        internal void UpdateItem(DownloadedElevationData item)
        {
            _database.UpdateItem(item);
            MainThread.BeginInvokeOnMainThread(() =>
                DownloadedElevationDataUpdated?.Invoke(this, new DownloadedElevationDataEventArgs() {data = item})
            );
        }

        internal void DeleteItem(DownloadedElevationData item)
        {
            _database.DeleteItem(item);
            MainThread.BeginInvokeOnMainThread(() =>
                DownloadedElevationDataDeleted?.Invoke(this, new DownloadedElevationDataEventArgs() {data = item})
            );
        }
    }
}