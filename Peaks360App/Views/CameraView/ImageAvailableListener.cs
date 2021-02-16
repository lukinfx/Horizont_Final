using Android.Media;
using Peaks360App.AppContext;
using Peaks360App.DataAccess;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Models;
//using Java.IO;
using Java.Lang;
using Java.Nio;
using Java.Nio.FileNio;
using System;
using System.IO;
//using System.IO;

namespace Peaks360App.Views.Camera
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public ImageAvailableListener(CameraFragment fragment)
        {
            if (fragment == null)
                throw new System.ArgumentNullException("fragment");
            
            owner = fragment;
        }


        private IAppContext _context;
        private readonly Java.IO.File file;
        private readonly CameraFragment owner;
        private GpsLocation _location;
        private double _heading;

        //public File File { get; private set; }
        //public CameraFragment Owner { get; private set; }

        public void OnImageAvailable(ImageReader reader)
        {
            ImageSaver imageSaver = new ImageSaver(reader.AcquireNextImage(), _context);
            owner.mBackgroundHandler.Post(imageSaver);
        }

        // Saves a JPEG {@link Image} into the specified {@link File}.
        
        public void SetContext(IAppContext context)
        {
            _context = context;
        }
    }
}