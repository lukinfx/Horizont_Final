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
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;

namespace HorizontApp.Models
{
    public class PhotoAddedEventArgs : EventArgs { public PhotoData data; }
    public delegate void PhotoAddedEventHandler(object sender, PhotoAddedEventArgs e);

    public class PhotosModel
    {
        private PoiDatabase _database;
        
        public event PhotoAddedEventHandler PhotoAdded;

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
            PhotoAdded?.Invoke(this, new PhotoAddedEventArgs() {data = item});
        }


    }
}