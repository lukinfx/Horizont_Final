using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Models
{
    public class PhotoDataEventArgs : EventArgs { public PhotoData data; }
    public delegate void PhotoDataEventHandler(object sender, PhotoDataEventArgs e);

    public class PhotosModel
    {
        private PoiDatabase _database;
        
        public event PhotoDataEventHandler PhotoAdded;
        public event PhotoDataEventHandler PhotoUpdated;

        public PhotosModel(PoiDatabase database)
        {
            _database = database;
        }

        public IEnumerable<PhotoData> GetPhotoDataItems()
        {
            return _database.GetPhotoDataItems();
        }

        public void InsertItem(PhotoData item)
        {
            _database.InsertItem(item);
            PhotoAdded?.Invoke(this, new PhotoDataEventArgs() {data = item});
        }

        internal void UpdateItem(PhotoData item)
        {
            _database.UpdateItem(item);
            PhotoUpdated?.Invoke(this, new PhotoDataEventArgs() { data = item });
        }
    }
}