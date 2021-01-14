using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using HorizontApp.AppContext;
using HorizontApp.DataAccess;
using HorizontApp.Tasks;
using HorizontApp.Utilities;
using HorizontApp.Views;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HorizontLib.Utilities;
using Xamarin.Essentials;
using static Android.Views.View;
using GpsUtils = HorizontApp.Utilities.GpsUtils;
using HorizontApp.Views.ScaleImage;
using Xamarin.Forms;
using AbsoluteLayout = Android.Widget.AbsoluteLayout;
using ImageButton = Android.Widget.ImageButton;
using Rect = Android.Graphics.Rect;
using View = Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : HorizonBaseActivity
    {
        public static int REQUEST_SHOW_PHOTO = 0;

        private static string TAG = "Horizon-PhotoShowActivity";

        private TextView _GPSTextView;
        private TextView _headingTextView;
        private ScaleImageView photoView;

        private byte[] _thumbnail;

        private ImageButton _tiltCorrectorButton;

        private PhotoData photodata;

        private Bitmap dstBmp;

        private PoiViewItem _selectedPoi;


        private AppContextStaticData _context;
        protected override IAppContext Context { get { return _context; } }

        void InitializeAppContext(PhotoData photodata)
        {
            Log.WriteLine(LogPriority.Debug, TAG, $"Heading {photodata.Heading:F0}");

            /*if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                heading += 90;
            }*/

            var loc = new GpsLocation(photodata.Longitude, photodata.Latitude, photodata.Altitude);
            if (AppContextLiveData.Instance.Settings.AltitudeFromElevationMap)
            {
                var elevationTile = new ElevationTile(loc);
                if (elevationTile.Exists())
                {
                    if (elevationTile.LoadFromZip())
                    {
                        loc.Altitude = elevationTile.GetElevation(loc);
                    }
                }
            }

            _context = new AppContextStaticData(loc, photodata.Heading);

            //### This can be removed later
            if (photodata.PictureWidth == 0) photodata.PictureWidth = AppContextLiveData.Instance.Settings.CameraPictureSize.Width;
            if (photodata.PictureHeight == 0) photodata.PictureHeight = AppContextLiveData.Instance.Settings.CameraPictureSize.Height;

            Context.Settings.LoadData(this);
            Context.Settings.IsViewAngleCorrection = false;
            Context.Settings.Categories = JsonConvert.DeserializeObject<List<PoiCategory>>(photodata.JsonCategories);
            Context.Settings.SetCameraParameters((float)photodata.ViewAngleHorizontal, (float)photodata.ViewAngleVertical,
                photodata.PictureWidth, photodata.PictureHeight);
            Context.Settings.MaxDistance = Convert.ToInt32(photodata.MaxDistance);
            Context.Settings.MinAltitute = Convert.ToInt32(photodata.MinAltitude);
            Context.Settings.ShowElevationProfile = photodata.ShowElevationProfile;
            Context.ElevationProfileDataDistance = photodata.MaxElevationProfileDataDistance;
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (AppContextLiveData.Instance.IsPortrait)
            {
                SetContentView(Resource.Layout.PhotoShowActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.PhotoShowActivityLandscape);
            }

            long id = Intent.GetLongExtra("ID", -1);
            photodata = Database.GetPhotoDataItem(id);

            InitializeAppContext(photodata);
            InitializeBaseActivityUI();

            _thumbnail = photodata.Thumbnail;

            _headingTextView = FindViewById<TextView>(Resource.Id.editText1);
            _GPSTextView = FindViewById<TextView>(Resource.Id.editText2);

            FindViewById<ImageButton>(Resource.Id.menuButton).SetOnClickListener(this);

            var _saveToDeviceButton = FindViewById<ImageButton>(Resource.Id.buttonSaveToDevice);
            _saveToDeviceButton.SetOnClickListener(this);

            var _shareButton = FindViewById<ImageButton>(Resource.Id.buttonShare);
            _shareButton.SetOnClickListener(this);

            _tiltCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonTiltCorrector);
            _tiltCorrectorButton.SetOnClickListener(this);

            photoView = FindViewById<ScaleImageView>(Resource.Id.photoView);

            _compassView.Initialize(Context, false,
                new System.Drawing.Size(GetPictureWidth(), photodata.PictureHeight),
                (float)photodata.LeftTiltCorrector, (float)photodata.RightTiltCorrector, 0);

            if (_thumbnail != null)
            {
                var bitmap = BitmapFactory.DecodeByteArray(_thumbnail, 0, _thumbnail.Length);
                MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(bitmap); });
            }

            var delayedAction = new System.Threading.Timer(o => { LoadImageAndProfile(); },
                null, TimeSpan.FromSeconds(0.1), TimeSpan.FromMilliseconds(-1));

            Start();
        }

        private void LoadImageAndProfile()
        {
            LoadImage(photodata.PhotoFileName);

            if (Context.Settings.ShowElevationProfile)
            {
                if (photodata.JsonElevationProfileData != null)
                {
                    Context.ElevationProfileData = ElevationProfileData.Deserialize(photodata.JsonElevationProfileData);
                    if (Context.ElevationProfileData != null)
                    {
                        RefreshElevationProfile();
                    }
                }
            }
        }

        void LoadImage(string fileName)
        {
            var path = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), fileName);
            /*
            var a = BitmapDrawable.CreateFromPath(path);
            MainThread.BeginInvokeOnMainThread(() => { photoLayout.SetBackground(a); });
            */

            try
            {

                using (FileStream fs = System.IO.File.OpenRead(path))
                {
                    byte[] b;
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        b = br.ReadBytes((int)fs.Length);
                    }
                    var bmp = BitmapFactory.DecodeByteArray(b, 0, b.Length);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        photoView.SetScaleType(ImageView.ScaleType.CenterCrop);
                        photoView.SetImageBitmap(bmp);
                    });

                    dstBmp = bmp.Copy(Bitmap.Config.Argb8888, true);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public override void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            base.OnDataChanged(sender, e);
            
            Log.WriteLine(LogPriority.Debug, TAG, $"PoiCount: {e.PoiData.Count}");
            _compassView.SetPoiViewItemList(e.PoiData);
        }

        private void OnMaxDistanceChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (Context.Settings.ShowElevationProfile)
            {
                if (Context.ElevationProfileData == null && photodata.JsonElevationProfileData != null)
                {
                    Context.ElevationProfileData = JsonConvert.DeserializeObject<ElevationProfileData>(photodata.JsonElevationProfileData);
                }

                if (Context.ElevationProfileData == null || Context.ElevationProfileData.MaxDistance < Context.Settings.MaxDistance)
                {
                    GenerateElevationProfile();
                }
                else
                {
                    RefreshElevationProfile();
                }
            }

        }

        protected override void UpdateStatusBar()
        {
            var gpsLocation = GpsUtils.HasLocation(Context.MyLocation) ?
                $"Lat:{Context.MyLocation.Latitude:F7} Lon:{Context.MyLocation.Longitude:F7} Alt:{Context.MyLocation.Altitude:F0}"
                : "No GPS location";

            var sign = _compassView.HeadingCorrector < 0 ? '-' : '+';
            var heading = $"Hdg:{Context.Heading:F1}{sign}{Math.Abs(_compassView.HeadingCorrector):F1}";

            var zoomAndTiltCorrection = $"Scale:{photoView.Scale:F2} ,LT:{_compassView.LeftTiltCorrector:F2}, RT:{_compassView.RightTiltCorrector:F2}";

            var viewAngle = $"va-V:{ Context.ViewAngleVertical:F1} va-H:{ Context.ViewAngleHorizontal:F1}";

            var photoMatrix = $"im-X:{photoView.TranslateX:F1}, im-Y:{photoView.TranslateY:F1}, Sc:{photoView.DisplayScale:F2}/{photoView.Scale:F2}";

            //_GPSTextView.Text = heading + "  /  " + zoomAndTiltCorrection + "  /  " + viewAngle;// + "  /  " + photoMatrix;
            _GPSTextView.Text = zoomAndTiltCorrection + "  /  " + viewAngle + "  /  " + photoMatrix;
        }


        protected override void OnMove(int distanceX, int distanceY)
        {
            photoView.MoveTo(distanceX, distanceY);
            photoView.Cutting();

            _compassView.Move(photoView.DisplayTranslateX, photoView.DisplayTranslateY);
            Log.WriteLine(LogPriority.Debug, TAG, $"Moving: {distanceX}/{distanceY}");
        }

        protected override void OnZoom(float scale, int x, int y)
        {
            photoView.ZoomTo(scale, x, y);
            photoView.Cutting();

            _compassView.RecalculateViewAngles(photoView.DisplayScale);
            _compassView.Move(photoView.DisplayTranslateX, photoView.DisplayTranslateY);
            Log.WriteLine(LogPriority.Debug, TAG, $"Zooming: {scale}");
        }

        public override void OnClick(View v)
        {
            base.OnClick(v);

            switch (v.Id)
            {
                case Resource.Id.menuButton:
                    _saveData();

                    var resultIntent = new Intent();
                    resultIntent.PutExtra("Id", photodata.Id);
                    SetResult(Result.Ok, resultIntent);
                    Finish();
                    break;

                case Resource.Id.buttonTiltCorrector:
                    ToggleEditing();
                    _tiltCorrectorButton.SetImageResource(EditingOn?Resource.Drawable.ic_lock_unlocked: Resource.Drawable.ic_lock_locked);
                    break;

                case Resource.Id.buttonSaveToDevice:
                    _handleButtonSaveClicked();
                    break;

                case Resource.Id.buttonShare:
                    _handleButtonShareClicked();
                    break;
            }
        }

        private void _saveData()
        {
            //### This can be removed later
            photodata.PictureWidth = dstBmp.Width;
            photodata.PictureHeight = dstBmp.Height;

            photodata.MaxDistance = MaxDistance;
            photodata.MinAltitude = MinHeight;
            photodata.ViewAngleHorizontal = Context.ViewAngleHorizontal;
            photodata.ViewAngleVertical = Context.ViewAngleVertical;
            photodata.LeftTiltCorrector = _compassView.LeftTiltCorrector;
            photodata.RightTiltCorrector = _compassView.RightTiltCorrector;
            photodata.Heading = Context.Heading + _compassView.HeadingCorrector;
            photodata.ShowElevationProfile = Context.Settings.ShowElevationProfile;
            photodata.JsonCategories = JsonConvert.SerializeObject(Context.Settings.Categories);
            if (Context.ElevationProfileData != null)
                photodata.JsonElevationProfileData = Context.ElevationProfileData.Serialize();
            Database.UpdateItem(photodata);
        }

        private void _handleButtonSaveClicked()
        {
            var bmp = Bitmap.CreateBitmap(dstBmp);
            Canvas canvas = new Canvas(bmp);
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo_horizon5);

            var compassView = new CompassView(ApplicationContext, null);
            compassView.Initialize(Context, false,
                new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight),
                (float)_compassView.LeftTiltCorrector, (float)_compassView.RightTiltCorrector, (float)_compassView.HeadingCorrector);
            compassView.Layout(0, 0, photodata.PictureWidth, photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight));

            compassView.Draw(canvas);


            var logoWidth = Convert.ToInt32(0.2 * canvas.Width);
            canvas.DrawBitmap(logoBmp, new Rect(0, 0, logoBmp.Width, logoBmp.Height), new Rect(canvas.Width - logoWidth, canvas.Height - logoWidth * 2 / 3, canvas.Width, canvas.Height), null);
            var photoname = "export" +
                "" + photodata.PhotoFileName;
            var filename = System.IO.Path.Combine(ImageSaver.GetPublicPhotosFileFolder(), photoname);

            if (File.Exists(filename))
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton("Yes", (senderAlert, args) =>
                {
                    File.Delete(filename);
                    var stream = new FileStream(filename, FileMode.CreateNew);
                    bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] { filename }, null, null);
                    PopupHelper.InfoDialog(this, "Information", $"Photo saved.");
                });
                alert.SetNegativeButton("No", (senderAlert, args) =>
                {

                });
                alert.SetMessage($"This photo already exists. Do you want to rewrite it?");
                var answer = alert.Show();
            }
            else
            {
                var stream = new FileStream(filename, FileMode.CreateNew);
                bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] { filename }, null, null);
                PopupHelper.InfoDialog(this, "Information", $"Photo saved.");
            }
        }

        private void _handleButtonShareClicked()
        {
            var bmp = Bitmap.CreateBitmap(dstBmp);
            Canvas canvas = new Canvas(bmp);
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo_horizon5);

            var compassView = new CompassView(ApplicationContext, null);
            compassView.Initialize(Context, false,
                new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight),
                (float)_compassView.LeftTiltCorrector, (float)_compassView.RightTiltCorrector, (float)_compassView.HeadingCorrector);
            compassView.Layout(0, 0, photodata.PictureWidth, photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight));
            compassView.Draw(canvas);

            var logoWidth = Convert.ToInt32(0.2 * canvas.Width);
            canvas.DrawBitmap(logoBmp, new Rect(0, 0, logoBmp.Width, logoBmp.Height), new Rect(canvas.Width - logoWidth, canvas.Height - logoWidth * 2 / 3, canvas.Width, canvas.Height), null);
            //canvas.DrawBitmap(logoBmp, canvas.Width - logoBmp.Width - 40, canvas.Height - logoBmp.Height - 40, null);

            var filename = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), "tmpHorizon.jpg");

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            var stream = new FileStream(filename, FileMode.CreateNew);

            bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
            Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] { filename }, null, null);

            var result = Share.RequestAsync(new ShareFileRequest
            {
                Title = Title,
                File = new ShareFile(filename)
            });
        }

        #region Required abstract methods

        public bool OnDoubleTap(MotionEvent e)
        {
            float scale;
            if (photoView.DisplayScale < 1.1)
            {//Zoom in
                scale = photoView.MiddleScale / photoView.Scale;

            }
            else
            {//Zoom out
                scale = photoView.StdScale / photoView.Scale;
            }

            OnZoom(scale, (int)e.GetX(), (int)e.GetY());
            _compassView.Move(photoView.DisplayTranslateX, photoView.DisplayTranslateY);

            return false;
        }

        public bool OnDoubleTapEvent(MotionEvent e)
        {
            return false;
        }

        protected override int GetScreenWidth()
        {
            return photoView.Width;
        }

        protected override int GetScreenHeight()
        {
            return photoView.Height;
        }

        protected override int GetPictureWidth()
        {
            return photodata.PictureWidth;
        }

        protected override int GetPictureHeight()
        {
            return photodata.PictureHeight;
        }

        #endregion Required abstract methods
    }
}