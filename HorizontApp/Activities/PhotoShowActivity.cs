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
    public class PhotoShowActivity : Activity, IOnClickListener
    {
        public static int REQUEST_SHOW_PHOTO = 0;

        private static string TAG = "Horizon-PhotoShowActivity";

        private IAppContext _context;
        private TextView _GPSTextView;
        private TextView _headingTextView;
        private ScaleImageView photoView;
        private CompassView _compassView;
        private byte[] _thumbnail;
        private TextView _filterText;

        private ImageButton _favouriteButton;
        private ImageButton _displayTerrainButton;
        private ImageButton _tiltCorrectorButton;

        private LinearLayout _seekBars;
        private LinearLayout _poiInfo;

        private bool _editingOn = false;

        private bool _elevationProfileBeingGenerated = false;

        private SeekBar _distanceSeekBar;
        private SeekBar _heightSeekBar;
        private PhotoData photodata;

        private Bitmap dstBmp;

        //for gesture detection
        private int m_PreviousMoveX;
        private int m_PreviousMoveY;
        private int m_FirstMoveX;
        private int m_FirstMoveY;
        private float m_PreviousDistance;
        private float m_PreviousDistanceX;
        private float m_PreviousDistanceY;
        private bool m_IsScaling;
        private int m_startTime;
        private int m_tapCount = 0;


        private PoiViewItem _selectedPoi;

        private TapGestureRecognizer _tapGestureRecognizer;

        private PoiDatabase _database;
        private PoiDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new PoiDatabase();
                }
                return _database;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PhotoShowActivityLayout);
            long id = Intent.GetLongExtra("ID", -1);
            photodata = Database.GetPhotoDataItem(id);
            _thumbnail = photodata.Thumbnail;

            Log.WriteLine(LogPriority.Debug, TAG, $"Heading {photodata.Heading:F0}");

            /*if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                heading += 90;
            }*/

            var loc = new GpsLocation(
                photodata.Longitude,
                photodata.Latitude,
                photodata.Altitude);

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


            _context.Settings.LoadData(this);
            _context.Settings.IsViewAngleCorrection = false;
            _context.Settings.Categories = JsonConvert.DeserializeObject<List<PoiCategory>>(photodata.JsonCategories);
            _context.Settings.SetCameraParameters((float)photodata.ViewAngleHorizontal, (float)photodata.ViewAngleVertical,
                photodata.PictureWidth, photodata.PictureHeight);
            _context.Settings.MaxDistance = Convert.ToInt32(photodata.MaxDistance);
            _context.Settings.MinAltitute = Convert.ToInt32(photodata.MinAltitude);
            _context.Settings.ShowElevationProfile = photodata.ShowElevationProfile;
            _context.ElevationProfileDataDistance = photodata.MaxElevationProfileDataDistance;

            _filterText = FindViewById<TextView>(Resource.Id.textView1);

            _headingTextView = FindViewById<TextView>(Resource.Id.editText1);
            _GPSTextView = FindViewById<TextView>(Resource.Id.editText2);

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = _context.Settings.MaxDistance;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;
            _heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            _heightSeekBar.Progress = _context.Settings.MinAltitute;
            _heightSeekBar.ProgressChanged += OnMinAltitudeChanged;

            _seekBars = FindViewById<LinearLayout>(Resource.Id.mainActivitySeekBars);
            _poiInfo = FindViewById<LinearLayout>(Resource.Id.mainActivityPoiInfo);
            _seekBars.Visibility = ViewStates.Visible;
            _poiInfo.Visibility = ViewStates.Gone;

            _displayTerrainButton = FindViewById<ImageButton>(Resource.Id.buttonDisplayTerrain);
            _displayTerrainButton.SetOnClickListener(this);
            _displayTerrainButton.SetImageResource(_context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            _favouriteButton = FindViewById<ImageButton>(Resource.Id.favouriteFilterButton);
            _favouriteButton.SetOnClickListener(this);

            var _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            var _backButton = FindViewById<ImageButton>(Resource.Id.menuButton);
            _backButton.SetOnClickListener(this);

            var _saveToDeviceButton = FindViewById<ImageButton>(Resource.Id.buttonSaveToDevice);
            _saveToDeviceButton.SetOnClickListener(this);

            var _shareButton = FindViewById<ImageButton>(Resource.Id.buttonShare);
            _shareButton.SetOnClickListener(this);

            _tiltCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonTiltCorrector);
            _tiltCorrectorButton.SetOnClickListener(this);

            FindViewById<ImageButton>(Resource.Id.buttonWiki).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.buttonMap).SetOnClickListener(this);

            photoView = FindViewById<ScaleImageView>(Resource.Id.photoView);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.LayoutChange += OnLayoutChanged;
            _compassView.Initialize(_context, false,
                new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight), 
                (float)photodata.LeftTiltCorrector, (float)photodata.RightTiltCorrector, 0);
            
            var photoLayout = FindViewById<AbsoluteLayout>(Resource.Id.photoLayout);


            if (_thumbnail != null)
            {
                var bitmap = BitmapFactory.DecodeByteArray(_thumbnail, 0, _thumbnail.Length);
                //var number = ((float)DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Height) / ((float)bitmap.Height / bitmap.Width) * bitmap.Width;
                /*var bmp = Bitmap.CreateBitmap(bitmap,
                                Convert.ToInt32(number) / 2, 0,
                                Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Width),
                                Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height));*/
                MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(bitmap); });
            }

            //System.Threading.Tasks.Task.Run(() => {LoadImage(fileName); });

            var delayedAction = new System.Threading.Timer(o => { LoadImageAndProfile(); },
                null, TimeSpan.FromSeconds(0.1), TimeSpan.FromMilliseconds(-1));

            //Finnaly setup OnDataChanged listener and Road all data
            _context.DataChanged += OnDataChanged;
        }

        public void OnLayoutChanged(object sender, LayoutChangeEventArgs e)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                _context.ReloadData();
            });
        }

        private void LoadImageAndProfile()
        {
            LoadImage(photodata.PhotoFileName);

            if (_context.Settings.ShowElevationProfile)
            {
                if (photodata.JsonElevationProfileData != null)
                {
                    _context.ElevationProfileData = ElevationProfileData.Deserialize(photodata.JsonElevationProfileData);
                    if (_context.ElevationProfileData != null)
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

        public void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _filterText.Visibility = ViewStates.Invisible;

                UpdateStatusBar();

                Log.WriteLine(LogPriority.Debug, TAG, $"PoiCount: {e.PoiData.Count}");
                _compassView.SetPoiViewItemList(e.PoiData);
            });
        }

        private void UpdateStatusBar()
        {
            var gpsLocation = GpsUtils.HasLocation(_context.MyLocation) ?
                $"Lat:{_context.MyLocation.Latitude:F7} Lon:{_context.MyLocation.Longitude:F7} Alt:{_context.MyLocation.Altitude:F0}" 
                : "No GPS location";

            var sign = _compassView.HeadingCorrector < 0 ? '-' : '+';
            var heading = $"Hdg:{_context.Heading:F1}{sign}{Math.Abs(_compassView.HeadingCorrector):F1}";

            var zoomAndTiltCorrection = $"Scale:{photoView.Scale:F2} ,LT:{_compassView.LeftTiltCorrector:F2}, RT:{_compassView.RightTiltCorrector:F2}";

            var viewAngle = $"va-V:{ _context.ViewAngleVertical:F1} va-H:{ _context.ViewAngleHorizontal:F1}";

            var photoMatrix = $"im-X:{photoView.TranslateX:F1}, im-Y:{photoView.TranslateY:F1}, Sc:{photoView.DisplayScale:F2}/{photoView.Scale:F2}";

            //_GPSTextView.Text = heading + "  /  " + zoomAndTiltCorrection + "  /  " + viewAngle;// + "  /  " + photoMatrix;
            _GPSTextView.Text = zoomAndTiltCorrection + "  /  " + viewAngle + "  /  " + photoMatrix;
        }

        private void OnMinAltitudeChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            _filterText.Text = "vyska nad " + _heightSeekBar.Progress + "m, do " + _distanceSeekBar.Progress + "km daleko";
            _filterText.Visibility = ViewStates.Visible;

            _context.Settings.MinAltitute = _heightSeekBar.Progress;
        }

        private void OnMaxDistanceChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            //TODO: Save minAltitude and maxDistance to CompassViewSettings
            _filterText.Text = "vyska nad " + _heightSeekBar.Progress + "m, do " + _distanceSeekBar.Progress + "km daleko";
            _filterText.Visibility = ViewStates.Visible;

            _context.Settings.MaxDistance = _distanceSeekBar.Progress;

            if (_context.Settings.ShowElevationProfile)
            {
                if (_context.ElevationProfileData == null && photodata.JsonElevationProfileData != null)
                {
                    _context.ElevationProfileData = JsonConvert.DeserializeObject<ElevationProfileData>(photodata.JsonElevationProfileData);
                }

                if (_context.ElevationProfileData == null || _context.ElevationProfileData.MaxDistance < _distanceSeekBar.Progress)
                {
                    GenerateElevationProfile();
                    _context.ElevationProfileDataDistance = _distanceSeekBar.Progress;
                }
                else
                {
                    RefreshElevationProfile();
                }
            }

        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (_editingOn)
            {
                if (e1.RawX < Resources.DisplayMetrics.WidthPixels / 7)
                {
                    _compassView.OnScroll(distanceY, true);
                }
                else if (e1.RawX > Resources.DisplayMetrics.WidthPixels - Resources.DisplayMetrics.WidthPixels / 7)
                {
                    _compassView.OnScroll(distanceY, false);
                }
                else if (e1.RawY < 0.75 * Resources.DisplayMetrics.HeightPixels)
                {
                    _compassView.OnScroll(distanceX);
                }
            }
            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            base.OnTouchEvent(e);

            var touchCount = e.PointerCount;
            switch (e.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Pointer1Down:
                case MotionEventActions.Pointer2Down:
                {

                    m_FirstMoveX = m_PreviousMoveX = (int) e.GetX();
                    m_FirstMoveY = m_PreviousMoveY = (int) e.GetY();

                    if (touchCount == 1)
                    {

                        if (System.Environment.TickCount - m_startTime > 500)
                        {
                            m_tapCount = 0;
                        }

                        if (m_tapCount == 0)
                        {
                            m_startTime = System.Environment.TickCount;
                        }

                        m_tapCount++;
                    }

                    if (touchCount >= 2)
                    {
                        m_PreviousDistance = photoView.Distance(e.GetX(0), e.GetX(1), e.GetY(0), e.GetY(1));
                        m_PreviousDistanceX = Math.Abs(e.GetX(0) - e.GetX(1));
                        m_PreviousDistanceY = Math.Abs(e.GetY(0) - e.GetY(1));

                        m_IsScaling = true;
                    }
                }
                    break;
                case MotionEventActions.Move:
                {
                    //heading and tilt correction
                    if (_editingOn && touchCount == 1)
                    {
                        var distanceX = m_PreviousMoveX - (int) e.GetX();
                        var distanceY = m_PreviousMoveY - (int) e.GetY();
                        m_PreviousMoveX = (int) e.GetX();
                        m_PreviousMoveY = (int) e.GetY();

                        if (Math.Abs(m_FirstMoveX - e.GetX()) > Math.Abs(m_FirstMoveY - e.GetY()))
                        {
                            _compassView.OnScroll(distanceX);
                            Log.WriteLine(LogPriority.Debug, TAG, $"Heading correction: {distanceX}");
                        }
                        else
                        {
                            if (e.RawX < Resources.DisplayMetrics.WidthPixels / 2)
                            {
                                _compassView.OnScroll(distanceY, true);
                                Log.WriteLine(LogPriority.Debug, TAG, $"Left tilt correction: {distanceY}");
                            }
                            else
                            {
                                _compassView.OnScroll(distanceY, false);
                                Log.WriteLine(LogPriority.Debug, TAG, $"Right tilt correction: {distanceY}");
                            }
                        }
                    }
                    //zooming
                    else if (touchCount >= 2 && m_IsScaling && !_editingOn)
                    {
                        var distance = photoView.Distance(e.GetX(0), e.GetX(1), e.GetY(0), e.GetY(1));
                        var scale = (distance - m_PreviousDistance) / photoView.DispDistance();
                        m_PreviousDistance = distance;
                        scale += 1;
                        scale = scale * scale;
                        photoView.ZoomTo(scale, photoView.Width / 2, photoView.Height / 2);
                        photoView.Cutting();

                        _compassView.RecalculateViewAngles(photoView.DisplayScale);
                        _compassView.Move(photoView.DisplayTranslateX, photoView.DisplayTranslateY);
                        Log.WriteLine(LogPriority.Debug, TAG, $"Zooming: {scale}");
                    }
                    //moving
                    else if (!m_IsScaling && photoView.Scale > photoView.MinScale && !_editingOn)
                    {
                        var distanceX = m_PreviousMoveX - (int) e.GetX();
                        var distanceY = m_PreviousMoveY - (int) e.GetY();
                        m_PreviousMoveX = (int) e.GetX();
                        m_PreviousMoveY = (int) e.GetY();
                        photoView.MoveTo(-distanceX, -distanceY);
                        photoView.Cutting();

                        _compassView.Move(photoView.DisplayTranslateX, photoView.DisplayTranslateY);
                        Log.WriteLine(LogPriority.Debug, TAG, $"Moving: {distanceX}/{distanceY}");
                    }
                    else if (touchCount >= 2 && _editingOn)
                    {
                        var distX = Math.Abs(e.GetX(0) - e.GetX(1));
                        var distY = Math.Abs(e.GetY(0) - e.GetY(1));
                        if (distX > distY)
                        {
                            var scale = (distX - m_PreviousDistanceX) / photoView.Width;
                            m_PreviousDistanceX = distX;
                            scale += 1;
                            _compassView.ScaleHorizontalViewAngle(scale);
                            Log.WriteLine(LogPriority.Debug, TAG, $"Horizontal VA correction: {scale}");
                        }
                        else
                        {
                            var scale = (distY - m_PreviousDistanceY) / photoView.Height;
                            m_PreviousDistanceY = distY;
                            scale += 1;
                            _compassView.ScaleVerticalViewAngle(scale);
                            Log.WriteLine(LogPriority.Debug, TAG, $"Vertical VA correction: {scale}");
                        }
                    }
                    break;
                }
                case MotionEventActions.Up:
                case MotionEventActions.Pointer1Up:
                case MotionEventActions.Pointer2Up:
                {
                    //zooming by double tap
                    if (touchCount == 1)
                    {
                        if (m_tapCount == 2)
                        {
                            long time = System.Environment.TickCount - m_startTime;

                            if (time < 500)
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

                                photoView.ZoomTo(scale, photoView.Width / 2, photoView.Height / 2);
                                photoView.Cutting();

                                _compassView.RecalculateViewAngles(photoView.DisplayScale);

                                //TODO: Moving to place of double tap gesture
                                //photoView.MoveTo(x, y);

                                _compassView.Move(photoView.DisplayTranslateX, photoView.DisplayTranslateY);
                            }

                            m_tapCount = 0;
                            break;
                        }
                    }

                    if (touchCount <= 1)
                    {
                        if (m_IsScaling)
                        {
                            m_IsScaling = false;
                        }
                        else
                        {
                            //seelecting POI item
                            var dist = photoView.Distance(e.GetX(0), m_FirstMoveX, e.GetY(0), m_FirstMoveY);
                            if (dist < 1)
                            {
                                var newSelectedPoi = _compassView.GetPoiByScreenLocation(e.GetX(0), e.GetY(0));

                                if (_selectedPoi != null)
                                {
                                    _selectedPoi.Selected = false;
                                }

                                if (newSelectedPoi != null)
                                {
                                    _selectedPoi = newSelectedPoi;
                                    _selectedPoi.Selected = true;

                                    _seekBars.Visibility = ViewStates.Gone;
                                    _poiInfo.Visibility = ViewStates.Visible;
                                    FindViewById<TextView>(Resource.Id.textViewPoiName).Text = _selectedPoi.Poi.Name;
                                    FindViewById<TextView>(Resource.Id.textViewPoiDescription).Text = "No description";
                                    FindViewById<TextView>(Resource.Id.textViewPoiGpsLocation).Text = $"{_selectedPoi.Poi.Altitude} m / {(_selectedPoi.GpsLocation.Distance / 1000):F2} km";
                                    FindViewById<TextView>(Resource.Id.textViewPoiData).Text = $"{_selectedPoi.Poi.Latitude:F7} N, {_selectedPoi.Poi.Longitude:F7} E";
                                    FindViewById<ImageButton>(Resource.Id.buttonWiki).Visibility = WikiUtilities.HasWiki(_selectedPoi.Poi) ? ViewStates.Visible : ViewStates.Gone;
                                }
                                else
                                {
                                    _selectedPoi = null;

                                    _seekBars.Visibility = ViewStates.Visible;
                                    _poiInfo.Visibility = ViewStates.Gone;
                                }
                                _compassView.Invalidate();
                            }
                        }
                    }
                    break;
                }
            }

            UpdateStatusBar();
            return true;
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonDisplayTerrain:
                    HandleDisplayTarrainButtonClicked();
                    break;

                case Resource.Id.favouriteFilterButton:
                    {
                        _context.Settings.ToggleFavourite(); ;
                        if (_context.Settings.ShowFavoritesOnly)
                            _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2_on);
                        else
                            _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2);
                        _context.ReloadData();
                        break;
                    }
                case Resource.Id.buttonCategorySelect:
                    {
                        var dialog = new PoiFilterDialog(this, _context);
                        dialog.Show();
                       
                        break;
                    }
                case Resource.Id.menuButton:
                    {
                        _saveData();

                        var resultIntent = new Intent();
                        resultIntent.PutExtra("Id", photodata.Id);
                        SetResult(Result.Ok, resultIntent);
                        Finish();
                        break;
                    }
                case Resource.Id.buttonTiltCorrector:
                    {
                        _editingOn = !_editingOn;
                        if (_editingOn)
                            _tiltCorrectorButton.SetImageResource(Resource.Drawable.ic_lock_unlocked);
                        else if (!_editingOn)
                            _tiltCorrectorButton.SetImageResource(Resource.Drawable.ic_lock_locked);
                        break;
                    }
                case Resource.Id.buttonSaveToDevice:

                    _handleButtonSaveClicked();
                    break;
                case Resource.Id.buttonShare:

                    _handleButtonShareClicked();
                    break;
                case Resource.Id.buttonMap:
                    MapUtilities.OpenMap(_selectedPoi.Poi);
                    break;
                case Resource.Id.buttonWiki:
                    WikiUtilities.OpenWiki(_selectedPoi.Poi);
                    break;

            }
        }

        private void _saveData()
        {
            //### This can be removed later
            photodata.PictureWidth = dstBmp.Width;
            photodata.PictureHeight = dstBmp.Height;

            photodata.MaxDistance = _distanceSeekBar.Progress;
            photodata.MinAltitude = _heightSeekBar.Progress;
            photodata.ViewAngleHorizontal = _context.ViewAngleHorizontal;
            photodata.ViewAngleVertical = _context.ViewAngleVertical; 
            photodata.LeftTiltCorrector = _compassView.LeftTiltCorrector;
            photodata.RightTiltCorrector = _compassView.RightTiltCorrector;
            photodata.Heading = _context.Heading + _compassView.HeadingCorrector;
            photodata.ShowElevationProfile = _context.Settings.ShowElevationProfile;
            photodata.JsonCategories = JsonConvert.SerializeObject(_context.Settings.Categories);
            if (_context.ElevationProfileData != null)
                photodata.JsonElevationProfileData = _context.ElevationProfileData.Serialize();
            Database.UpdateItem(photodata);
        }

        private void _handleButtonSaveClicked()
        {
            var bmp = Bitmap.CreateBitmap(dstBmp);
            Canvas canvas = new Canvas(bmp);
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo_horizon5);

            var compassView = new CompassView(ApplicationContext, null);
            compassView.Initialize(_context, false,
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
            compassView.Initialize(_context, false,
                new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight),
                (float)_compassView.LeftTiltCorrector, (float)_compassView.RightTiltCorrector, (float)_compassView.HeadingCorrector);
            compassView.Layout(0, 0, photodata.PictureWidth, photodata.PictureHeight);
            compassView.InitializeViewDrawer(new System.Drawing.Size(dstBmp.Width, dstBmp.Height), new System.Drawing.Size(photodata.PictureWidth, photodata.PictureHeight));
            compassView.Draw(canvas);

            var logoWidth = Convert.ToInt32(0.2 * canvas.Width);
            canvas.DrawBitmap(logoBmp, new Rect(0, 0, logoBmp.Width, logoBmp.Height), new Rect(canvas.Width - logoWidth, canvas.Height  - logoWidth * 2 / 3, canvas.Width, canvas.Height), null);
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
        
        #region ElevationProfile
        private void HandleDisplayTarrainButtonClicked()
        {
            _context.Settings.ShowElevationProfile = !_context.Settings.ShowElevationProfile;
            _displayTerrainButton.SetImageResource(_context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            CheckAndReloadElevationProfile();
        }

        private void CheckAndReloadElevationProfile()
        {
            if (_context.Settings.ShowElevationProfile)
            {
                if (GpsUtils.HasAltitude(_context.MyLocation))
                {
                    if (_elevationProfileBeingGenerated == false)
                    {
                        if (_context.ElevationProfileData == null || !_context.ElevationProfileData.IsValid(_context.MyLocation, _context.Settings.MaxDistance))
                        {
                            GenerateElevationProfile();
                        }
                    }
                }
            }

            _compassView.Invalidate();
        }

        private void GenerateElevationProfile()
        {
            try
            {
                if (!GpsUtils.HasAltitude(_context.MyLocation))
                {
                    PopupHelper.ErrorDialog(this, "Error", "It's not possible to generate elevation profile without known altitude");
                    return;
                }

                var ec = new ElevationCalculation(_context.MyLocation, _distanceSeekBar.Progress);

                var size = ec.GetSizeToDownload();
                if (size == 0)
                {
                    StartDownloadAndCalculate(ec);
                    return;
                }

                using (var builder = new AlertDialog.Builder(this))
                {
                    builder.SetTitle("Question");
                    builder.SetMessage($"This action requires to download additional {size} MBytes. Possibly set lower visibility to reduce amount of downloaded data. \r\n\r\nDo you really want to continue?");
                    builder.SetIcon(Android.Resource.Drawable.IcMenuHelp);
                    builder.SetPositiveButton("OK", (senderAlert, args) => { StartDownloadAndCalculateAsync(ec); });
                    builder.SetNegativeButton("Cancel", (senderAlert, args) => { });

                    var myCustomDialog = builder.Create();

                    myCustomDialog.Show();
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when generating elevation profile. {ex.Message}");
            }
        }

        private void StartDownloadAndCalculate(ElevationCalculation ec)
        {
            _elevationProfileBeingGenerated = true;
            var lastProgressUpdate = System.Environment.TickCount;

            var pd = new ProgressDialog(this);
            pd.SetMessage("Loading elevation data. Please Wait.");
            pd.SetCancelable(false);
            pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
            pd.Show();

            ec.OnFinishedAction = (result) =>
            {
                pd.Hide();
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    PopupHelper.ErrorDialog(this, "Error", result.ErrorMessage);
                }

                _context.ElevationProfileData = result;
                _elevationProfileBeingGenerated = false;
                RefreshElevationProfile();
            };
            ec.OnStageChange = (text, max) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    pd.SetMessage(text);
                    pd.Max = max;
                });
            };
            ec.OnProgressChange = (progress) =>
            {
                var tickCount = System.Environment.TickCount;
                if (tickCount - lastProgressUpdate > 100)
                {
                    MainThread.BeginInvokeOnMainThread(() => { pd.Progress = progress; });
                    Thread.Sleep(50);
                    lastProgressUpdate = tickCount;
                }
            };

            ec.Execute(_context.MyLocation);
        }

        private void StartDownloadAndCalculateAsync(ElevationCalculation ec)
        {
            try
            {
                StartDownloadAndCalculate(ec);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when generating elevation profile. {ex.Message}");
            }
        }

        private void RefreshElevationProfile()
        {
            if (_context.ElevationProfileData != null)
            {
                _compassView.SetElevationProfile(_context.ElevationProfileData);
            }
        }
        #endregion ElevationProfile
    }
}