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
using Xamarin.Essentials;
using Xamarin.Forms.Platform.Android;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : Activity
    {
        private IAppContext _context;
        private ImageView photoView;
        private CompassView _compassView;
        private byte[] _thumbnail;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PhotoShowActivityLayout);
            string fileName = Intent.GetStringExtra("name");
            _thumbnail = Intent.GetByteArrayExtra("thumbnail");
            _context = new AppContextStaticData(new GpsLocation(Intent.GetDoubleExtra("longitude", 0), Intent.GetDoubleExtra("latitude", 0), Intent.GetDoubleExtra("altitude", 0)), Intent.GetDoubleExtra("heading", 0));
            

            photoView = FindViewById<ImageView>(Resource.Id.photoView);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.Initialize(_context);

            var photoLayout = FindViewById<AbsoluteLayout>(Resource.Id.photoLayout);

            var delayedAction = new System.Threading.Timer(
                o => 
                {
                    var path = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), fileName);
                    /*
                    var a = BitmapDrawable.CreateFromPath(path);
                    MainThread.BeginInvokeOnMainThread(() => { photoLayout.SetBackground(a); });
                    */

                    try
                    {
                        /*if (_thumbnail != null)
                        {
                            var bitmap = BitmapFactory.DecodeByteArray(_thumbnail, 0, _thumbnail.Length);
                            MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(bitmap);});
                        }*/

                        using (FileStream fs = System.IO.File.OpenRead(path))
                        {
                            byte[] b;
                            using (BinaryReader br = new BinaryReader(fs))
                            {
                                b = br.ReadBytes((int)fs.Length);
                            }
                            var a = ImageResizer.ResizeImageAndroid(b, (float)photoView.Width, (float)photoView.Height, 100);
                            var bmp = BitmapFactory.DecodeByteArray(b, 0, b.Length);

                            var dstBmp = Bitmap.CreateBitmap(bmp, 
                                Convert.ToInt32(bmp.Width - photoView.Width)/2, 0, 
                                Convert.ToInt32(photoView.Width), 
                                Convert.ToInt32(photoView.Height));

                            MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(dstBmp); });
                            
                        }
                    } 
                    catch (Exception ex)
                    {

                    }
                },
                null, 
                TimeSpan.FromSeconds(1), 
                TimeSpan.FromMilliseconds(-1));

            // Create your application here


        }


    }
}