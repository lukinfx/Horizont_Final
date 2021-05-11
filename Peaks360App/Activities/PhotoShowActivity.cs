using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Newtonsoft.Json;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Extensions;
using Peaks360App.AppContext;
using Peaks360App.Providers;
using Peaks360App.Utilities;
using Peaks360App.Views;
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

        private static bool _firstStart = true;

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
        protected override bool TwoPointTiltCorrectionEnabled => EditingOn;
        protected override bool OnePointTiltCorrectionEnabled => false;
        protected override bool HeadingCorrectionEnabled => EditingOn;
        protected override bool ViewAngleCorrectionEnabled => EditingOn;

        protected override bool ImageCroppingEnabled => CroppingOn;

        void InitializeAppContext(PhotoData photodata)
        {
            var loc = new GpsLocation(photodata.Longitude, photodata.Latitude, photodata.Altitude);
            
            /* Fetching altitude from elevation map if available (this is not needed probably)
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
            }*/

            _context = new AppContextStaticData(loc, new PlaceInfo(), photodata.Heading);
            _context.LeftTiltCorrector = photodata.LeftTiltCorrector ?? 0;
            _context.RightTiltCorrector = photodata.RightTiltCorrector ?? 0;
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
            Log.WriteLine(LogPriority.Debug, TAG, "OnCreate - Enter");
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
            _compassView.Initialize(Context, false, pictureSize);

            var delayedAction = new System.Threading.Timer(o => { LoadImageAndProfile(); },
                null, TimeSpan.FromSeconds(0.1), TimeSpan.FromMilliseconds(-1));

            Start();
            Log.WriteLine(LogPriority.Debug, TAG, "OnCreate - Exit");
        }

        protected override void OnStart()
        {
            base.OnStart();

            /* Display thumbnail first (this is not needed probably)
            if (_photodata.Thumbnail != null)
            {
                var bitmap = BitmapFactory.DecodeByteArray(_photodata.Thumbnail, 0, _photodata.Thumbnail.Length);
                MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(bitmap); });
            }*/

            if (_firstStart)
            {
                TutorialDialog.ShowTutorial(this, TutorialPart.PhotoShowActivity,
                    new TutorialPage[]
                    {
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_photoedit_save, textResourceId = Resource.String.Tutorial_PhotoShow_Save},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_photoedit_share, textResourceId = Resource.String.Tutorial_PhotoShow_Share},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_photoedit_crop, textResourceId = Resource.String.Tutorial_PhotoShow_Crop},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_photoedit_tilt, textResourceId = Resource.String.Tutorial_PhotoShow_Tilt},
                    }, () => { _firstStart = false; });
            }
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
                        ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context);
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
            if (EditingOn)
            {
                SavePhotoData();
                _tiltCorrectorButton.SetImageResource(Resource.Drawable.ic_edit);
                EditingOn = false;
            }
            else
            {
                _tiltCorrectorButton.SetImageResource(Resource.Drawable.ic_save);
                EditingOn = true;

                TutorialDialog.ShowTutorial(this, TutorialPart.PhotoEditActivity,
                    new TutorialPage[] {
                        new TutorialPage() { imageResourceId = Resource.Drawable.tutorial_heading_correction, textResourceId = Resource.String.Tutorial_PhotoEdit_HeadingCorrection },
                        new TutorialPage() { imageResourceId = Resource.Drawable.tutorial_horizont_correction, textResourceId = Resource.String.Tutorial_PhotoEdit_HorizontCorrection },
                        new TutorialPage() { imageResourceId = Resource.Drawable.tutorial_viewangle_correction, textResourceId = Resource.String.Tutorial_PhotoEdit_ViewAngleCorrection },
                    });
            }
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
            try
            {
                base.OnDataChanged(sender, e);

                Log.WriteLine(LogPriority.Debug, TAG, $"PoiCount: {e.PoiData.Count}");
                _compassView.SetPoiViewItemList(e.PoiData);

                //TODO: check is the following call is really needed
                ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context);
            }
            catch (Exception ex)
            {
                //TODO: Possibly log the failure
            }
        }

        protected override void UpdateStatusBar()
        {
            string gpsLocation;
            if (GpsUtils.HasLocation(Context.MyLocation))
            {
                gpsLocation = $"GPS:{Context.MyLocation.LocationAsString()} Alt:{Context.MyLocation.Altitude:F0}";
            }
            else
            {
                gpsLocation = "No GPS location";
            }

            var sign = Context.HeadingCorrector < 0 ? '-' : '+';
            var heading = $"Hdg:{Context.Heading:F1}{sign}{Math.Abs(Context.HeadingCorrector):F1}";

            var zoomAndTiltCorrection = $"Scale:{photoView.Scale:F2} ,LT:{Context.LeftTiltCorrector:F2}, RT:{Context.RightTiltCorrector:F2}";

            var viewAngle = $"va-V:{Context.ViewAngleVertical:F1} va-H:{Context.ViewAngleHorizontal:F1}";

            var photoMatrix = $"im-X:{photoView.TranslateX:F1}, im-Y:{photoView.TranslateY:F1}, Sc:{photoView.DisplayScale:F2}/{photoView.Scale:F2}";

            //_GPSTextView.Text = heading + "  /  " + zoomAndTiltCorrection + "  /  " + viewAngle;// + "  /  " + photoMatrix;
            SetStatusLineText(zoomAndTiltCorrection + "  /  " + viewAngle + "  /  " + photoMatrix);
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
            if (photoView.Scale > photoView.StdScale * 1.1)
            {
                //the image is zoomed, so user is rather moving the image -> do not swap images
                return;
            }

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
            if (photoView.Scale > photoView.StdScale * 1.1)
            {
                //the image is zoomed, so user is rather moving the image -> do not swap images
                return;
            }

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

        protected override void OnCropAdjustment(CroppingHandle handle, float distX, float distY)
        {
            if (photoView.IsCropAllowed(handle, distX, distY))
            {
                var oldCR = photoView.CroppingRectangle;
                var distanceX = distX / photoView.Scale;
                var distanceY = distY / photoView.Scale;

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
                    if (photoView.IsCroppedImageTooSmall())
                    {
                        PopupHelper.Toast(this, Resource.String.PhotoShow_ImageTooSmall);
                        return;
                    }
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
                AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
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
                AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Save), (senderAlert, args) => { SavePhotoData(); action.Invoke(); });
                alert.SetNegativeButton(Resources.GetText(Resource.String.Common_Discard), (senderAlert, args) => { action.Invoke(); }); 
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
                || !(_photodata.LeftTiltCorrector?.IsEqual(Context.LeftTiltCorrector, 0.01) ?? true)
                || !(_photodata.RightTiltCorrector?.IsEqual(Context.RightTiltCorrector, 0.01) ?? true)
                || !_photodata.Heading.IsEqual(Context.Heading + Context.HeadingCorrector, 0.1)
                || (_photodata.ShowElevationProfile && !elevationProfileData.MaxDistance.IsEqual(Context.ElevationProfileData.MaxDistance, 0.1));
        }

        private void SavePhotoData()
        {
            //### This can be removed later
            _photodata.PictureWidth = dstBmp.Width;
            _photodata.PictureHeight = dstBmp.Height;

            _photodata.MaxDistance = MaxDistance;
            _photodata.ViewAngleHorizontal = Context.ViewAngleHorizontal;
            _photodata.ViewAngleVertical = Context.ViewAngleVertical;
            _photodata.LeftTiltCorrector = Context.LeftTiltCorrector;
            _photodata.RightTiltCorrector = Context.RightTiltCorrector;
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
            var newPhotodata = ImageSaver.SaveCopy(dstBmp, photoView.CroppingRectangle, _photodata, MaxDistance, Context);
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

            _compassView.Initialize(Context, false, pictureSize);
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
            compassView.Initialize(Context, false, new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight));
            compassView.Layout(0, 0, _photodata.PictureWidth, _photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight));
            compassView.SetPoiViewItemList(Context.PoiData);
            compassView.SetElevationProfile(Context.ElevationProfileData);
            compassView.Draw(canvas);


            var logoWidth = Convert.ToInt32(0.2 * canvas.Width);
            canvas.DrawBitmap(logoBmp, new Rect(0, 0, logoBmp.Width, logoBmp.Height), new Rect(canvas.Width - logoWidth, canvas.Height - logoWidth * 2 / 3, canvas.Width, canvas.Height), null);
            var photoname = "export" +
                "" + _photodata.PhotoFileName;
            var filename = System.IO.Path.Combine(ImageSaverUtils.GetPublicPhotosFileFolder(), photoname);

            if (File.Exists(filename))
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
                {
                    File.Delete(filename);
                    var stream = new FileStream(filename, FileMode.CreateNew);
                    bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] {filename}, null, null);
                    PopupHelper.Toast(this, Resource.String.PhotoShow_PhotoSaved);
                });
                alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
                alert.SetMessage(Resources.GetText(Resource.String.PhotoShow_OverwriteQuestion));
                var answer = alert.Show();
            }
            else
            {
                var stream = new FileStream(filename, FileMode.CreateNew);
                bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] {filename}, null, null);
                PopupHelper.Toast(this, Resource.String.PhotoShow_PhotoSaved);
            }
        }

        private void _handleButtonShareClicked()
        {
            var bmp = Bitmap.CreateBitmap(dstBmp);
            Canvas canvas = new Canvas(bmp);
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo_horizon5);

            var compassView = new CompassView(ApplicationContext, null);
            compassView.Initialize(Context, false, new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight));
            compassView.Layout(0, 0, _photodata.PictureWidth, _photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(_photodata.PictureWidth, _photodata.PictureHeight));
            compassView.SetPoiViewItemList(Context.PoiData);
            compassView.SetElevationProfile(Context.ElevationProfileData);
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