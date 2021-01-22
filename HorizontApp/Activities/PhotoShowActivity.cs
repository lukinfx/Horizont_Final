using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using HorizontApp.AppContext;
using HorizontApp.Utilities;
using HorizontApp.Views;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using HorizonLib.Domain.ViewModel;
using HorizontLib.Utilities;
using Xamarin.Essentials;
using HorizontApp.Views.ScaleImage;
using GpsUtils = HorizontApp.Utilities.GpsUtils;
using ImageButton = Android.Widget.ImageButton;
using Rect = Android.Graphics.Rect;
using View = Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : HorizonBaseActivity
    {
        public static int REQUEST_SHOW_PHOTO = Definitions.BaseResultCode.PHOTO_SHOW_ACTIVITY;

        private static string TAG = "Horizon-PhotoShowActivity";

        private TextView _GPSTextView;
        private TextView _headingTextView;
        private ScaleImageView photoView;

        private byte[] _thumbnail;

        private ImageButton _tiltCorrectorButton;
        private ImageButton _cropButton;
        private LinearLayout _confirmCloseButtons;
        private LinearLayout _photoShowActivityControlBar;
        private LinearLayout _mainActivityStatusBar;

        private PhotoData photodata;

        private Bitmap dstBmp;

        private PoiViewItem _selectedPoi;


        private AppContextStaticData _context;
        protected override IAppContext Context { get { return _context; } }
        protected bool EditingOn { get; set; }
        protected bool CroppingOn { get; set; }

        protected override bool MoveingAndZoomingEnabled => !EditingOn;
        protected override bool TiltCorrectionEnabled => EditingOn;
        protected override bool HeadingCorrectionEnabled => EditingOn;
        protected override bool ViewAngleCorrectionEnabled => EditingOn;

        protected override bool ImageCroppingEnabled => CroppingOn;

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
            
            _context = new AppContextStaticData(loc, "unknown location", photodata.Heading);

            //### This can be removed later
            if (photodata.PictureWidth == 0) photodata.PictureWidth = AppContextLiveData.Instance.Settings.CameraPictureSize.Width;
            if (photodata.PictureHeight == 0) photodata.PictureHeight = AppContextLiveData.Instance.Settings.CameraPictureSize.Height;

            Context.Settings.LoadData(this);
            Context.Settings.IsViewAngleCorrection = false;
            Context.Settings.Categories = JsonConvert.DeserializeObject<List<PoiCategory>>(photodata.JsonCategories);
            Context.Settings.SetCameraParameters((float) photodata.ViewAngleHorizontal, (float) photodata.ViewAngleVertical,
                photodata.PictureWidth, photodata.PictureHeight);
            Context.Settings.MaxDistance = Convert.ToInt32(photodata.MaxDistance);
            Context.Settings.MinAltitute = Convert.ToInt32(photodata.MinAltitude);
            Context.ShowFavoritesOnly = photodata.FavouriteFilter;
            Context.Settings.ShowElevationProfile = photodata.ShowElevationProfile;
            if (photodata.JsonElevationProfileData != null)
            {
                Context.ElevationProfileData = ElevationProfileData.Deserialize(photodata.JsonElevationProfileData);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

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

            _cropButton = FindViewById<ImageButton>(Resource.Id.buttonCropImage);
            _cropButton.SetOnClickListener(this);

            _confirmCloseButtons = FindViewById<LinearLayout>(Resource.Id.confirmCloseButtons);
            _confirmCloseButtons.Visibility = ViewStates.Gone;
            FindViewById<ImageButton>(Resource.Id.confirmButton).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.closeButton).SetOnClickListener(this);

            _photoShowActivityControlBar = FindViewById<LinearLayout>(Resource.Id.PhotoShowActivityControlBar);
            _mainActivityStatusBar = FindViewById<LinearLayout>(Resource.Id.mainActivityStatusBar); 
            photoView = FindViewById<ScaleImageView>(Resource.Id.photoView);

            _compassView.Initialize(Context, false,
                new System.Drawing.Size(GetPictureWidth(), photodata.PictureHeight),
                (float)photodata.LeftTiltCorrector, (float)photodata.RightTiltCorrector);

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

        protected void ToggleEditing()
        {
            EditingOn = !EditingOn;

            _tiltCorrectorButton.SetImageResource(EditingOn ? Resource.Drawable.ic_lock_unlocked : Resource.Drawable.ic_lock_locked);
        }

        protected void ToggleCropping()
        {
            if (!CroppingOn)
            {
                EnableCropping();
            }
            else
            {
                DisableCropping();
            }
        }

        protected void EnableCropping()
        {
            CroppingOn = true;

            Matrix inverseMatrix = new Matrix();
            if (photoView.m_Matrix.Invert(inverseMatrix))
            {
                var cr = CroppingOn ? new RectF(
                    GetScreenWidth() / 5, GetScreenHeight() / 5,
                    GetScreenWidth() * 4 / 5, GetScreenHeight() * 4 / 5) : null;
                var dst = new RectF();
                inverseMatrix.MapRect(dst, cr);
                photoView.CroppingRectangle = new Rect((int)(int)dst.Left, (int)dst.Top, (int)dst.Right, (int)dst.Bottom);

                _confirmCloseButtons.Visibility = ViewStates.Visible;
                _seekBars.Visibility = ViewStates.Gone;
                _poiInfo.Visibility = ViewStates.Gone;
                _photoShowActivityControlBar.Visibility = ViewStates.Gone;
                _mainActivityStatusBar.Visibility = ViewStates.Gone;

                _compassView.ShowPointsOfInterest = false;
                _compassView.ShowElevationProfile = false;
            }
        }

        protected void DisableCropping()
        {
            CroppingOn = false;

            photoView.CroppingRectangle = null;

            _confirmCloseButtons.Visibility = ViewStates.Gone;
            _seekBars.Visibility = ViewStates.Visible;
            _photoShowActivityControlBar.Visibility = ViewStates.Visible;
            _mainActivityStatusBar.Visibility = ViewStates.Visible;

            _compassView.ShowPointsOfInterest = true;
            _compassView.ShowElevationProfile = Context.Settings.ShowElevationProfile;
        }

        public override void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            base.OnDataChanged(sender, e);
            
            Log.WriteLine(LogPriority.Debug, TAG, $"PoiCount: {e.PoiData.Count}");
            _compassView.SetPoiViewItemList(e.PoiData);
            
            CheckAndReloadElevationProfile();
        }

        protected override void UpdateStatusBar()
        {
            var gpsLocation = GpsUtils.HasLocation(Context.MyLocation) ?
                $"Lat:{Context.MyLocation.Latitude:F7} Lon:{Context.MyLocation.Longitude:F7} Alt:{Context.MyLocation.Altitude:F0}"
                : "No GPS location";

            var sign = Context.HeadingCorrector < 0 ? '-' : '+';
            var heading = $"Hdg:{Context.Heading:F1}{sign}{Math.Abs(Context.HeadingCorrector):F1}";

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

        protected override void OnCropAdjustment(CroppingHandle handle, float distanceX, float distanceY)
        {
            var oldCR = photoView.CroppingRectangle;
            distanceX = distanceX / photoView.Scale;
            distanceY = distanceY / photoView.Scale;

            switch (handle)
            {
                case CroppingHandle.Left:
                    photoView.CroppingRectangle = new Rect((int)(oldCR.Left + distanceX), oldCR.Top, oldCR.Right, oldCR.Bottom);
                    break;
                case CroppingHandle.Right:
                    photoView.CroppingRectangle = new Rect(oldCR.Left, oldCR.Top, (int)(oldCR.Right + distanceX), oldCR.Bottom);
                    break;
                case CroppingHandle.Top:
                    photoView.CroppingRectangle = new Rect(oldCR.Left, (int)(oldCR.Top + distanceY), oldCR.Right, oldCR.Bottom);
                    break;
                case CroppingHandle.Bottom:
                    photoView.CroppingRectangle = new Rect(oldCR.Left, oldCR.Top, oldCR.Right, (int)(oldCR.Bottom + distanceY)); 
                    break;
            }
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
                    break;

                case Resource.Id.buttonCropImage:
                    ToggleCropping();
                    break;

                case Resource.Id.confirmButton:
                    //PopupHelper.InfoDialog(this, "NotImplemented", "This feature is not implemented yet.");
                    SaveCopy(); 
                    DisableCropping(); 

                    break;
                
                case Resource.Id.closeButton:
                    DisableCropping();
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
            photodata.Heading = Context.Heading + Context.HeadingCorrector;
            photodata.ShowElevationProfile = Context.Settings.ShowElevationProfile;
            photodata.FavouriteFilter = Context.ShowFavoritesOnly;
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
                (float)_compassView.LeftTiltCorrector, (float)_compassView.RightTiltCorrector);
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
                (float)_compassView.LeftTiltCorrector, (float)_compassView.RightTiltCorrector);
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

        public override bool OnDoubleTap(MotionEvent e)
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

        protected override CroppingHandle? GetCroppingHandle(float x, float y)
        {
            return photoView.GetCroppingHandle(x, y);
        }

        #endregion Required abstract methods

        private void SaveCopy()
        {
            var cropRect = photoView.CroppingRectangle;

            var croppedBitmap = Bitmap.CreateBitmap(dstBmp, cropRect.Left, cropRect.Top, cropRect.Width(), cropRect.Height());

            var viewAngleVertical = Context.ViewAngleVertical * croppedBitmap.Height / (double)dstBmp.Height;
            var viewAngleHorizontal = Context.ViewAngleHorizontal * croppedBitmap.Width / (double)dstBmp.Width;

            //center new, minu center old
            var hdgCorrectionInPixels = ((cropRect.Left + cropRect.Right) / 2.0) - (dstBmp.Width / 2.0);
            //and now to degrees
            var hdgCorrectionInDegrees = hdgCorrectionInPixels / (float) dstBmp.Width * Context.ViewAngleHorizontal;

            //Linear function - calculation of new corrector values
            double leftCorrectionInDegrees = _compassView.LeftTiltCorrector + (_compassView.RightTiltCorrector - _compassView.LeftTiltCorrector) * (cropRect.Left / (double)dstBmp.Width);
            double rightCorrectionInDegrees = _compassView.LeftTiltCorrector + (_compassView.RightTiltCorrector - _compassView.LeftTiltCorrector) * (cropRect.Right / (double)dstBmp.Width);

            //New center of image can be somewhere else, so we need to reflect this in total view angle correction
            double totalCorrectionInPixels = ((cropRect.Top + cropRect.Bottom) / 2.0) - (dstBmp.Height / 2.0);
            double totalCorrectionInDegrees = totalCorrectionInPixels / (cropRect.Height() * viewAngleVertical);

            var now = DateTime.Now;

            PhotoData newPhotodata = new PhotoData
            {
                Tag = "Copy of " + photodata.Tag,
                Datetime = now,
                PhotoFileName = ImageSaver.GetPhotoFileName(now),
                Longitude = photodata.Longitude,
                Latitude = photodata.Latitude,
                Altitude = photodata.Altitude,
                Heading = Context.Heading + Context.HeadingCorrector + hdgCorrectionInDegrees,
                JsonCategories = JsonConvert.SerializeObject(Context.Settings.Categories),
                ViewAngleVertical = viewAngleVertical,
                ViewAngleHorizontal = viewAngleHorizontal,
                LeftTiltCorrector = leftCorrectionInDegrees + totalCorrectionInDegrees,
                RightTiltCorrector = rightCorrectionInDegrees + totalCorrectionInDegrees,
                PictureWidth = croppedBitmap.Width,
                PictureHeight = croppedBitmap.Height,
                MinAltitude = MinHeight,
                MaxDistance = MaxDistance,
                FavouriteFilter = Context.ShowFavoritesOnly,
                ShowElevationProfile = Context.Settings.ShowElevationProfile
            };

            var thumbnainBitmap = Bitmap.CreateScaledBitmap(croppedBitmap, 150, 100, false);
            using (MemoryStream ms = new MemoryStream())
            {
                thumbnainBitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, ms);
                newPhotodata.Thumbnail = ms.ToArray();
            }
            
            if (_context.ElevationProfileData != null)
            {
                photodata.JsonElevationProfileData = Context.ElevationProfileData.Serialize();
            }

            var filePath = System.IO.Path.Combine(ImageSaver.GetPhotosFileFolder(), newPhotodata.PhotoFileName);
            var stream = new FileStream(filePath, FileMode.Create);
            croppedBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

            AppContextLiveData.Instance.PhotosModel.InsertItem(newPhotodata);
        }
    }
}