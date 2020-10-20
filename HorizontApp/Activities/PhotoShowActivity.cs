using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.AppContext;
using HorizontApp.Utilities;
using HorizontApp.Views;
using HorizontLib.Domain.Models;
using Xamarin.Forms.Platform.Android;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : Activity
    {
        private IAppContext _context;
        private ImageView photoView;
        private CompassView _compassView;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PhotoShowActivityLayout);
            string fileName = Intent.GetStringExtra("name");
            _context = new AppContextStaticData(new GpsLocation(Intent.GetDoubleExtra("longitude", 0), Intent.GetDoubleExtra("latitude", 0), Intent.GetDoubleExtra("altitude", 0)), Intent.GetDoubleExtra("heading", 0));
            

            photoView = FindViewById<ImageView>(Resource.Id.photoView);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.Initialize(_context);

            var photoLayout = FindViewById<AbsoluteLayout>(Resource.Id.photoLayout);
            var path = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), fileName);
            var a = BitmapDrawable.CreateFromPath(path);
            photoLayout.SetBackground(a);
            
            
            try
            {
                using (FileStream fs = System.IO.File.OpenRead(path))
                {
                    var bitmap = BitmapFactory.DecodeStream(fs);
                    photoView.SetImageBitmap(bitmap);
                }
            }
            catch (Exception ex)
            { 
                
            }

            // Create your application here


        }


    }
}