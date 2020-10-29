﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
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
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Xaml;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : Activity, GestureDetector.IOnGestureListener, IOnClickListener
    {
        private static string TAG = "Horizon-PhotoShowActivity";

        private IAppContext _context;
        private TextView _GPSTextView;
        private TextView _headingTextView;
        private ImageView photoView;
        private CompassView _compassView;
        private byte[] _thumbnail;
        private GestureDetector _gestureDetector;
        private System.Timers.Timer _refreshTimer = new System.Timers.Timer();

        private TextView _filterText;

        private ImageButton _favouriteButton;
        private ImageButton _displayTerrainButton;
        private ImageButton _refreshCorrectorButton;
        private ImageButton _tiltCorrectorButton;

        private bool _tiltCorrectorOn = false;

        private SeekBar _distanceSeekBar;
        private SeekBar _heightSeekBar;
        private PhotoData photodata;

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

            var heading = photodata.Heading;

            Log.WriteLine(LogPriority.Debug, TAG, $"Heading {heading:F0}");

            /*if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                heading += 90;
            }*/

            _gestureDetector = new GestureDetector(this);

            _context = new AppContextStaticData(
                new GpsLocation(
                        photodata.Longitude,
                        photodata.Latitude,
                        photodata.Altitude),
                heading
                );

            _context.DataChanged += OnDataChanged;
            _context.Settings.LoadData(this);
            _context.Settings.Categories = JsonConvert.DeserializeObject<List<PoiCategory>>(photodata.JsonCategories);
            _context.Settings.SetCameraParameters((float)photodata.ViewAngleHorizontal, (float)photodata.ViewAngleVertical,
                AppContextLiveData.Instance.Settings.CameraPictureSize.Width, AppContextLiveData.Instance.Settings.CameraPictureSize.Height);

            _filterText = FindViewById<TextView>(Resource.Id.textView1);

            _headingTextView = FindViewById<TextView>(Resource.Id.editText1);
            _GPSTextView = FindViewById<TextView>(Resource.Id.editText2);

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = _context.Settings.MaxDistance;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;
            _heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            _heightSeekBar.Progress = _context.Settings.MinAltitute;
            _heightSeekBar.ProgressChanged += OnMinAltitudeChanged;

            _displayTerrainButton = FindViewById<ImageButton>(Resource.Id.buttonDisplayTerrain);
            _displayTerrainButton.SetOnClickListener(this);

            _refreshCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonResetCorrector);
            _refreshCorrectorButton.SetOnClickListener(this);

            _favouriteButton = FindViewById<ImageButton>(Resource.Id.favouriteFilterButton);
            _favouriteButton.SetOnClickListener(this);

            var _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            var _backButton = FindViewById<ImageButton>(Resource.Id.menuButton);
            _backButton.SetOnClickListener(this);

            _tiltCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonTiltCorrector);
            _tiltCorrectorButton.SetOnClickListener(this);
            

            photoView = FindViewById<ImageView>(Resource.Id.photoView);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.Initialize(_context);

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

            var delayedAction = new System.Threading.Timer(
                o => { LoadImage(photodata.PhotoFileName); },
                null, 
                TimeSpan.FromSeconds(0.1), 
                TimeSpan.FromMilliseconds(-1));

            System.Threading.Tasks.Task.Run(() => { _context.ReloadData(); });

            InitializeRefreshTimer();
        }

        private void InitializeRefreshTimer()
        {
            _refreshTimer.Interval = 100;
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.Enabled = true;
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
                    var a = ImageResizer.ResizeImageAndroid(b, (float)photoView.Width, (float)photoView.Height, 100);
                    var bmp = BitmapFactory.DecodeByteArray(a, 0, a.Length);

                    var dstBmp = Bitmap.CreateBitmap(bmp,
                        Convert.ToInt32(bmp.Width - photoView.Width) / 2, 0,
                        Convert.ToInt32(photoView.Width),
                        Convert.ToInt32(photoView.Height));

                    MainThread.BeginInvokeOnMainThread(() => { photoView.SetImageBitmap(dstBmp); });

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

                _GPSTextView.Text = GpsUtils.HasLocation(_context.MyLocation) ?
                    $"Lat:{_context.MyLocation.Latitude:F7} Lon:{_context.MyLocation.Longitude:F7} Alt:{_context.MyLocation.Altitude:F0}" : "No GPS location";
                Log.WriteLine(LogPriority.Debug, TAG, $"PoiCount: {e.PoiData.Count}");
                _compassView.SetPoiViewItemList(e.PoiData);

            });
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshHeading();
            });
        }

        private void RefreshHeading()
        {
            _compassView.Heading = _context.Heading + _compassView.HeadingCorrector;
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                //_headingEditText.Text = $"{Math.Round(Context.Heading, 0):F0}°+{_compassView.HeadingCorrector + 90:F0} | ";
            }
            else
            {
                //_headingEditText.Text = $"{Math.Round(Context.Heading, 0):F0}°+{_compassView.HeadingCorrector:F0} | ";
            }

            _headingTextView.Text = $"{Math.Round(_context.Heading, 0):F0}°+{_compassView.HeadingCorrector + 90:F0} | ";
            _compassView.Invalidate();
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
        }


        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (_tiltCorrectorOn)
            {
                bool isLeft;
                if (e1.RawX < Resources.DisplayMetrics.WidthPixels / 3)
                {
                    isLeft = true;
                    _compassView.OnScroll(distanceY, isLeft);
                }
                if (e1.RawX > (2 * Resources.DisplayMetrics.WidthPixels / 3))
                {

                    isLeft = false;
                    _compassView.OnScroll(distanceY, isLeft);
                }
            }
            if (!_tiltCorrectorOn && e1.RawY < Resources.DisplayMetrics.HeightPixels / 2)
                _compassView.OnScroll(distanceX);
            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            _gestureDetector.OnTouchEvent(e);
            return false;
        }
        
        #region Required abstract methods
        public bool OnDown(MotionEvent e) { return false; }
        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY) { return false; }
        public void OnLongPress(MotionEvent e) { }
        public void OnShowPress(MotionEvent e) { }
        public bool OnSingleTapUp(MotionEvent e) { return false; }
        #endregion Required abstract methods

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonDisplayTerrain:
                    GenerateElevationProfile();
                    break;

                case Resource.Id.favouriteFilterButton:
                    {
                        _context.Settings.ToggleFavourite(); ;
                        if (_context.Settings.Favourite)
                            _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2_on);
                        else
                            _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2);
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
                        Finish();
                        break;
                    }
                case Resource.Id.buttonTiltCorrector:
                    {
                        _tiltCorrectorOn = !_tiltCorrectorOn;
                        if (_tiltCorrectorOn)
                            _tiltCorrectorButton.SetImageResource(Resource.Drawable.ic_lock_unlocked);
                        else if (!_tiltCorrectorOn)
                            _tiltCorrectorButton.SetImageResource(Resource.Drawable.ic_lock_locked);
                        break;
                    }
            }


        }

        #region ElevationProfile
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
                    builder.SetMessage($"This action requires to download additional {size} MBytes. Possibly set lower visibility to reduce amount downloaded data. \r\n\r\nDo you really want to continue?");
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