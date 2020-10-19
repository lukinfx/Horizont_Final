using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.AppContext;
using HorizontLib.Domain.Models;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : Activity
    {
        private IAppContext _context = new AppContextStaticData(new GpsLocation(), 50);

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            /*var path = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), item.PhotoFileName);

            try
            {
                using (FileStream fs = System.IO.File.OpenRead(path))
                {
                    var bitmap = BitmapFactory.DecodeStream(fs);
                    var bitmapScalled = Bitmap.CreateScaledBitmap(bitmap, 150, 100, true);
                    ThumbnailImageView.SetImageBitmap(bitmapScalled);
                }
            }
            catch (Exception)
            { 
            }*/

            // Create your application here
        }
    }
}