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
using Xamarin.Essentials;

namespace Peaks360App.Models
{
    public class PhotoDataEventArgs : EventArgs { public PhotoData data; }
    public delegate void PhotoDataEventHandler(object sender, PhotoDataEventArgs e);

    public class PhotosModel
    {
        private PoiDatabase _database;
        
        public event PhotoDataEventHandler PhotoAdded;
        public event PhotoDataEventHandler PhotoUpdated;
        public event PhotoDataEventHandler PhotoDeleted;

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
            MainThread.BeginInvokeOnMainThread( () => 
                PhotoAdded?.Invoke(this, new PhotoDataEventArgs() { data = item })
            );
        }

        public void DeleteItem(PhotoData item)
        {
            System.IO.File.Delete(item.PhotoFileName);
            _database.DeleteItem(item);
            MainThread.BeginInvokeOnMainThread(() =>
                PhotoDeleted?.Invoke(this, new PhotoDataEventArgs() {data = item})
            );
        }

        internal void UpdateItem(PhotoData item)
        {
            _database.UpdateItem(item);
            MainThread.BeginInvokeOnMainThread(() =>
                PhotoUpdated?.Invoke(this, new PhotoDataEventArgs() {data = item})
            );
        }
    }
}