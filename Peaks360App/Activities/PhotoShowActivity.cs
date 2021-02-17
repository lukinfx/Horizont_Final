using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using Peaks360App.Views;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Extensions;
using Peaks360Lib.Utilities;
using Xamarin.Essentials;
using Peaks360App.Views.ScaleImage;
using GpsUtils = Peaks360App.Utilities.GpsUtils;
using ImageButton = Android.Widget.ImageButton;
using Rect = Android.Graphics.Rect;
using View = Android.Views.View;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/PhotosActivity")]
    public class PhotoShowActivity : HorizonBaseActivity
    {
        public static int REQUEST_SHOW_PHOTO = Definitions.BaseResultCode.PHOTO_SHOW_ACTIVITY;

        private static string TAG = "Horizon-PhotoShowActivity";

        private TextView _GPSTextView;
        private ScaleImageView photoView;

        private ImageButton _tiltCorrectorButton;
        private ImageButton _cropButton;
        private ImageButton _saveToDeviceButton;
        private ImageButton _shareButton;

        private LinearLayout _confirmCloseButtons;
        private LinearLayout _mainActivityStatusBar;

        private PhotoData _photodata;

        private Bitmap dstBmp;

        private AppContextStaticData _context;

        protected override IAppContext Context
        {
            get { return _context; }
        }

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


            //Elavation data will be loaded when user will enable showing of elevation data
            if (photodata.JsonElevationProfileData == null) photodata.ShowElevationProfile = false;

            Context.Settings.LoadData(this);
            Context.Settings.IsViewAngleCorrection = false;
            Context.Settings.Categories = JsonConvert.DeserializeObject<List<PoiCategory>>(photodata.JsonCategories);
            Context.Settings.SetCameraParameters((float) photodata.ViewAngleHorizontal, (float) photodata.ViewAngleVertical,
                photodata.PictureWidth, photodata.PictureHeight);
            Context.Settings.MaxDistance = Convert.ToInt32(photodata.MaxDistance);
            Context.Settings.MinAltitute = Convert.ToInt32(photodata.MinAltitude);
            Context.ShowFavoritesOnly = photodata.FavouriteFilter;
            Context.Settings.ShowElevationProfile = photodata.ShowElevationProfile;
            Context.ElevationProfileData = ElevationProfileData.Deserialize(photodata.JsonElevationProfileData);
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
            _photodata = Database.GetPhotoDataItem(id);

            InitializeAppContext(_photodata);
            InitializeBaseActivityUI();

            _GPSTextView = FindViewById<TextView>(Resource.Id.editText2);

            FindViewById<ImageButton>(Resource.Id.menuButton).SetOnClickListener(this);

            _saveToDeviceButton = FindViewById<ImageButton>(Resource.Id.buttonSaveToDevice);
            _saveToDeviceButton.SetOnClickListener(this);

            _shareButton = FindViewById<ImageButton>(Resource.Id.buttonShare);
            _shareButton.SetOnClickListener(this);

            _tiltCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonTiltCorrector);
            _tiltCorrectorButton.SetOnClickListener(this);

            _cropButton = FindViewById<ImageButton>(Resource.Id.buttonCropImage);
            _cropButton.SetOnClickListener(this);

            _confirmCloseButtons = FindViewById<LinearLayout>(Resource.Id.confirmCloseButtons);
            _confirmCloseButtons.Visibility = ViewStates.Gone;
            FindViewById<ImageButton>(Resource.Id.confirmButton).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.closeButton).SetOnClickListener(this);

            _activityControlBar = FindViewById<LinearLayout>(Resource.Id.PhotoShowActivityControlBar);
            _mainActivityStatusBar = FindViewById<LinearLayout>(Resource.Id.mainActivityStatusBar);
            photoView = FindViewById<ScaleImageView>(Resource.Id.photoView);

            HideControls();

            var pictureSize = new System.Drawing.Size(GetPictureWidth(), GetPictureHeight());
            _compassView.Initialize(Context, false, pictureSize, (float?) _photodata.LeftTiltCorrector ?? 0, (float?) _photodata.RightTiltCorrector ?? 0);

            if (_photodata.Thumbnail != null)
            {
                var bitmap = BitmapFactory.DecodeByteArray(_photodata.Thumbnail, 0, _photodata.Thumbnail.Length);
                MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(bitmap); });
            }

            var delayedAction = new System.Threading.Timer(o => { LoadImageAndProfile(); },
                null, TimeSpan.FromSeconds(0.1), TimeSpan.FromMilliseconds(-1));

            Start();
        }

        private void LoadImageAndProfile()
        {
            LoadImage(_photodata.PhotoFileName);

            if (Context.Settings.ShowElevationProfile)
            {
                if (_photodata.JsonElevationProfileData != null)
                {
                    Context.ElevationProfileData = ElevationProfileData.Deserialize(_photodata.JsonElevationProfileData);
                    if (Context.ElevationProfileData != null)
                    {
                        RefreshElevationProfile();
                    }
                }
            }
        }

        void LoadImage(string fileName)
        {
            var path = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), fileName);
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
                        b = br.ReadBytes((int) fs.Length);
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

        protected void EnableCropping()
        {
            CroppingOn = true;

            Matrix inverseMatrix = new Matrix();
            if (photoView.m_Matrix.Invert(inverseMatrix))
            {
                var cr = CroppingOn
                    ? new RectF(
                        GetScreenWidth() / 5, GetScreenHeight() / 5,
                        GetScreenWidth() * 4 / 5, GetScreenHeight() * 4 / 5)
                    : null;
                var dst = new RectF();
                inverseMatrix.MapRect(dst, cr);
                photoView.CroppingRectangle = new Rect((int) (int) dst.Left, (int) dst.Top, (int) dst.Right, (int) dst.Bottom);

                _confirmCloseButtons.Visibility = ViewStates.Visible;
                _seekBars.Visibility = ViewStates.Gone;
                _poiInfo.Visibility = ViewStates.Gone;
                _activityControlBar.Visibility = ViewStates.Gone;
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
            _activityControlBar.Visibility = ViewStates.Visible;
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
            var gpsLocation = GpsUtils.HasLocation(Context.MyLocation)
                ? $"Lat:{Context.MyLocation.Latitude:F7} Lon:{Context.MyLocation.Longitude:F7} Alt:{Context.MyLocation.Altitude:F0}"
                : "No GPS location";

            var sign = Context.HeadingCorrector < 0 ? '-' : '+';
            var heading = $"Hdg:{Context.Heading:F1}{sign}{Math.Abs(Context.HeadingCorrector):F1}";

            var zoomAndTiltCorrection = $"Scale:{photoView.Scale:F2} ,LT:{_compassView.LeftTiltCorrector:F2}, RT:{_compassView.RightTiltCorrector:F2}";

            var viewAngle = $"va-V:{Context.ViewAngleVertical:F1} va-H:{Context.ViewAngleHorizontal:F1}";

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

        protected override void OnPreviousImage()
        {
            CheckDirtyAndPerformAction(() => ShowPreviousImage());
        }


        protected override void OnNextImage()
        {
            CheckDirtyAndPerformAction(() => ShowNextImage());
        }

        protected void ShowPreviousImage()
        {
            var newPhotodata = Context.Database.GetPreviousPhotoDataItem(_photodata);

            if (newPhotodata != null)
            {
                photoView.SetImageBitmap(null);
                _compassView.Visibility = ViewStates.Gone;
                photoView.Visibility = ViewStates.Gone;

                ReInitialize(newPhotodata);

                _compassView.Visibility = ViewStates.Visible;
                photoView.Visibility = ViewStates.Visible;
                photoView.InitializeTransformationMatrix();
            }
        }

        protected void ShowNextImage()
        {
            var newPhotodata = Context.Database.GetNextPhotoDataItem(_photodata);

            if (newPhotodata != null)
            {
                photoView.SetImageBitmap(null);
                _compassView.Visibility = ViewStates.Gone;
                photoView.Visibility = ViewStates.Gone;

                ReInitialize(newPhotodata);

                _compassView.Visibility = ViewStates.Visible;
                photoView.Visibility = ViewStates.Visible;
                photoView.InitializeTransformationMatrix();
            }
        }

        protected override void OnCropAdjustment(CroppingHandle handle, float distanceX, float distanceY)
        {
            var oldCR = photoView.CroppingRectangle;
            distanceX = distanceX / photoView.Scale;
            distanceY = distanceY / photoView.Scale;

            switch (handle)
            {
                case CroppingHandle.Left:
                    photoView.CroppingRectangle = new Rect((int) (oldCR.Left + distanceX), oldCR.Top, oldCR.Right, oldCR.Bottom);
                    break;
                case CroppingHandle.Right:
                    photoView.CroppingRectangle = new Rect(oldCR.Left, oldCR.Top, (int) (oldCR.Right + distanceX), oldCR.Bottom);
                    break;
                case CroppingHandle.Top:
                    photoView.CroppingRectangle = new Rect(oldCR.Left, (int) (oldCR.Top + distanceY), oldCR.Right, oldCR.Bottom);
                    break;
                case CroppingHandle.Bottom:
                    photoView.CroppingRectangle = new Rect(oldCR.Left, oldCR.Top, oldCR.Right, (int) (oldCR.Bottom + distanceY));
                    break;
            }
        }

        public override void OnClick(View v)
        {
            base.OnClick(v);

            switch (v.Id)
            {
                case Resource.Id.menuButton:
                    OnBackPressed();
                    break;

                case Resource.Id.buttonTiltCorrector:
                    ToggleEditing();
                    break;

                case Resource.Id.buttonCropImage:
                    EnableCropping();
                    break;

                case Resource.Id.confirmButton:
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

        public override void OnBackPressed()
        {
            CheckDirtyAndPerformAction(() => base.OnBackPressed());
            /*if (IsDirty())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Discard), (senderAlert, args) => { Finish(); });
                alert.SetNegativeButton(Resources.GetText(Resource.String.Save), (senderAlert, args) => { SavePhotoData(); Finish(); });
                alert.SetMessage(Resources.GetText(Resource.String.PhotoShow_SaveOrDiscard));
                var answer = alert.Show();
            }
            else
            {
                base.OnBackPressed();
            }*/
        }

        private void CheckDirtyAndPerformAction(Action action)
        {
            if (IsDirty())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Discard), (senderAlert, args) => { action.Invoke(); });
                alert.SetNegativeButton(Resources.GetText(Resource.String.Save), (senderAlert, args) => { SavePhotoData(); action.Invoke(); });
                alert.SetMessage(Resources.GetText(Resource.String.PhotoShow_SaveOrDiscard));
                var answer = alert.Show();
            }
            else
            {
                action.Invoke();
            }
        }

        private bool IsDirty()
        {
            var elevationProfileData = ElevationProfileData.Deserialize(_photodata?.JsonElevationProfileData);

            return
                _photodata.ShowElevationProfile != Context.Settings.ShowElevationProfile
                || _photodata.FavouriteFilter != Context.ShowFavoritesOnly
                || !_photodata.MaxDistance.IsEqual(Context.Settings.MaxDistance, 0.1)
                || !_photodata.MinAltitude.IsEqual(Context.Settings.MinAltitute, 0.1)
                || !_photodata.ViewAngleHorizontal.IsEqual(Context.ViewAngleHorizontal, 0.1)
                || !_photodata.ViewAngleVertical.IsEqual(Context.ViewAngleVertical, 0.1)
                || !(_photodata.LeftTiltCorrector?.IsEqual(_compassView.LeftTiltCorrector, 0.01) ?? true)
                || !(_photodata.RightTiltCorrector?.IsEqual(_compassView.RightTiltCorrector, 0.01) ?? true)
                || !_photodata.Heading.IsEqual(Context.Heading + Context.HeadingCorrector, 0.1)
                || (_photodata.ShowElevationProfile && !elevationProfileData.MaxDistance.IsEqual(Context.ElevationProfileData.MaxDistance, 0.1));
        }

        private void SavePhotoData()
        {
            //### This can be removed later
            _photodata.PictureWidth = dstBmp.Width;
            _photodata.PictureHeight = dstBmp.Height;

            _photodata.MaxDistance = MaxDistance;
            _photodata.MinAltitude = MinHeight;
            _photodata.ViewAngleHorizontal = Context.ViewAngleHorizontal;
            _photodata.ViewAngleVertical = Context.ViewAngleVertical;
            _photodata.LeftTiltCorrector = _compassView.LeftTiltCorrector;
            _photodata.RightTiltCorrector = _compassView.RightTiltCorrector;
            _photodata.Heading = Context.Heading + Context.HeadingCorrector;
            _photodata.ShowElevationProfile = Context.Settings.ShowElevationProfile;
            _photodata.FavouriteFilter = Context.ShowFavoritesOnly;
            _photodata.JsonCategories = JsonConvert.SerializeObject(Context.Settings.Categories);
            if (Context.ElevationProfileData != null)
                _photodata.JsonElevationProfileData = Context.ElevationProfileData.Serialize();
            AppContextLiveData.Instance.PhotosModel.UpdateItem(_photodata);
        }

        private void SaveCopy()
        {
            var newPhotodata = ImageCopySaver.Save(dstBmp, photoView.CroppingRectangle, _photodata,
                _compassView.LeftTiltCorrector, _compassView.RightTiltCorrector, MinHeight, MaxDistance, Context);
            AppContextLiveData.Instance.PhotosModel.InsertItem(newPhotodata);

            ReInitialize(newPhotodata);
        }

        private void ReInitialize(PhotoData newPhotodata)
        {
            //We also have to re-initialize AppContext, CompassView and CompassViewDrawer
            InitializeAppContext(newPhotodata);

            _photodata = newPhotodata;

            var pictureSize = new System.Drawing.Size(GetPictureWidth(), GetPictureHeight());
            var drawingSize = new System.Drawing.Size(_compassView.Width, _compassView.Height);

            _compassView.Initialize(Context, false, pictureSize, (float?)newPhotodata.LeftTiltCorrector ?? 0, (float?)newPhotodata.RightTiltCorrector ?? 0);
            _compassView.InitializeViewDrawer(drawingSize, pictureSize);

            
            LoadImageAndProfile();

            Start();
        }

        private void _handleButtonSaveClicked()
        {
            var bmp = Bitmap.CreateBitmap(dstBmp);
            Canvas canvas = new Canvas(bmp);
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo_horizon5);

            var compassView = new CompassView(ApplicationContext, null);
            compassView.Initialize(Context, false,
                new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight),
                (float) _compassView.LeftTiltCorrector, (float) _compassView.RightTiltCorrector);
            compassView.Layout(0, 0, _photodata.PictureWidth, _photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight));

            compassView.Draw(canvas);


            var logoWidth = Convert.ToInt32(0.2 * canvas.Width);
            canvas.DrawBitmap(logoBmp, new Rect(0, 0, logoBmp.Width, logoBmp.Height), new Rect(canvas.Width - logoWidth, canvas.Height - logoWidth * 2 / 3, canvas.Width, canvas.Height), null);
            var photoname = "export" +
                "" + _photodata.PhotoFileName;
            var filename = System.IO.Path.Combine(ImageSaverUtils.GetPublicPhotosFileFolder(), photoname);

            if (File.Exists(filename))
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) =>
                {
                    File.Delete(filename);
                    var stream = new FileStream(filename, FileMode.CreateNew);
                    bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] {filename}, null, null);
                    PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.Information), Resources.GetText(Resource.String.PhotoShow_PhotoSaved));
                });
                alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) => { });
                alert.SetMessage(Resources.GetText(Resource.String.PhotoShow_OverwriteQuestion));
                var answer = alert.Show();
            }
            else
            {
                var stream = new FileStream(filename, FileMode.CreateNew);
                bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] {filename}, null, null);
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
                new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight),
                (float) _compassView.LeftTiltCorrector, (float) _compassView.RightTiltCorrector);
            compassView.Layout(0, 0, _photodata.PictureWidth, _photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight));
            compassView.Draw(canvas);

            var logoWidth = Convert.ToInt32(0.2 * canvas.Width);
            canvas.DrawBitmap(logoBmp, new Rect(0, 0, logoBmp.Width, logoBmp.Height), new Rect(canvas.Width - logoWidth, canvas.Height - logoWidth * 2 / 3, canvas.Width, canvas.Height), null);
            //canvas.DrawBitmap(logoBmp, canvas.Width - logoBmp.Width - 40, canvas.Height - logoBmp.Height - 40, null);

            var filename = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), "tmpHorizon.jpg");

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            var stream = new FileStream(filename, FileMode.CreateNew);

            bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
            Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] {filename}, null, null);

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
            {
                //Zoom in
                scale = photoView.MiddleScale / photoView.Scale;

            }
            else
            {
                //Zoom out
                scale = photoView.StdScale / photoView.Scale;
            }

            OnZoom(scale, (int) e.GetX(), (int) e.GetY());
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
            return _photodata.PictureWidth;
        }

        protected override int GetPictureHeight()
        {
            return _photodata.PictureHeight;
        }

        protected override CroppingHandle? GetCroppingHandle(float x, float y)
        {
            return photoView.GetCroppingHandle(x, y);
        }

        #endregion Required abstract methods

    }
}