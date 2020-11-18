﻿using Android.App;
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
using System.Threading.Tasks;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : Activity, GestureDetector.IOnGestureListener, IOnClickListener
    {
        public static int REQUEST_SHOW_PHOTO = 0;

        private static string TAG = "Horizon-PhotoShowActivity";

        private IAppContext _context;
        private TextView _GPSTextView;
        private TextView _headingTextView;
        private ImageView photoView;
        private CompassView _compassView;
        private byte[] _thumbnail;
        private GestureDetector _gestureDetector;
        private TextView _filterText;

        private ImageButton _favouriteButton;
        private ImageButton _displayTerrainButton;
        private ImageButton _tiltCorrectorButton;

        private bool _editingOn = false;

        private bool _elevationProfileBeingGenerated = false;

        private SeekBar _distanceSeekBar;
        private SeekBar _heightSeekBar;
        private PhotoData photodata;

        private Bitmap dstBmp;

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

            _gestureDetector = new GestureDetector(this);

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

            _context.DataChanged += OnDataChanged;
            _context.Settings.LoadData(this);
            _context.Settings.Categories = JsonConvert.DeserializeObject<List<PoiCategory>>(photodata.JsonCategories);
            _context.Settings.SetCameraParameters((float)photodata.ViewAngleHorizontal, (float)photodata.ViewAngleVertical,
                AppContextLiveData.Instance.Settings.CameraPictureSize.Width, AppContextLiveData.Instance.Settings.CameraPictureSize.Height);
            _context.Settings.MaxDistance = Convert.ToInt32(photodata.MaxDistance);
            _context.Settings.MinAltitute = Convert.ToInt32(photodata.MinAltitude);

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
            
            

            photoView = FindViewById<ImageView>(Resource.Id.photoView);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.Initialize(_context);


            if (photodata.LeftTiltCorrector.HasValue && photodata.RightTiltCorrector.HasValue)
            {
                _compassView.OnScroll((float)-photodata.LeftTiltCorrector.Value, true);
                _compassView.OnScroll((float)-photodata.RightTiltCorrector.Value, false);
            
            }

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

            System.Threading.Tasks.Task.Run(() => { _context.ReloadData(); });

        }

        private void LoadImageAndProfile()
        {
            LoadImage(photodata.PhotoFileName);

            if (AppContextLiveData.Instance.Settings.AutoElevationProfile)
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

                    /*var a = ImageResizer.ResizeImageAndroid(b, (float)photoView.Width, (float)photoView.Height, 100);
                    var bmp = BitmapFactory.DecodeByteArray(a, 0, a.Length);
                    Bitmap bitmap;
                    if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
                    {
                        bitmap = Bitmap.CreateBitmap(bmp,
                            Convert.ToInt32(bmp.Width - photoView.Width) / 2, 0,
                            Convert.ToInt32(photoView.Width),
                            Convert.ToInt32(photoView.Height));
                    }
                    else
                    {
                        bitmap = Bitmap.CreateBitmap(bmp,
                            0, Convert.ToInt32(bmp.Height - photoView.Height) / 2,
                            Convert.ToInt32(photoView.Width),
                            Convert.ToInt32(photoView.Height));
                    }
                    dstBmp = Bitmap.CreateBitmap(bitmap);*/
                    

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

                _GPSTextView.Text = GpsUtils.HasLocation(_context.MyLocation) ?
                    $"Lat:{_context.MyLocation.Latitude:F7} Lon:{_context.MyLocation.Longitude:F7} Alt:{_context.MyLocation.Altitude:F0}" : "No GPS location";
                Log.WriteLine(LogPriority.Debug, TAG, $"PoiCount: {e.PoiData.Count}");
                _compassView.SetPoiViewItemList(e.PoiData);

            });
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

            /*if (photodata.MaxElevationProfileDataDistance < _distanceSeekBar.Progress)
            {
                GenerateElevationProfile();  
            }*/

            _context.Settings.MaxDistance = _distanceSeekBar.Progress;
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
                    HandleDisplayTarrainButtonClicked();
                    break;

                case Resource.Id.favouriteFilterButton:
                    {
                        _context.Settings.ToggleFavourite(); ;
                        if (_context.Settings.ShowFavoritesOnly)
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
                        photodata.LeftTiltCorrector = _compassView.GetTiltSettings().Item1;
                        photodata.RightTiltCorrector = _compassView.GetTiltSettings().Item2;
                        photodata.Heading = _context.Heading + _compassView.HeadingCorrector;
                        if (_context.ElevationProfileData != null)
                            photodata.JsonElevationProfileData = _context.ElevationProfileData.Serialize();
                        Database.UpdateItem(photodata);

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
            }


        }

        private void _handleButtonSaveClicked()
        {
            var bmp = Bitmap.CreateBitmap(dstBmp);
            Canvas canvas = new Canvas(bmp);
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo100px);

            _compassView.Draw(canvas);
            canvas.DrawBitmap(logoBmp, canvas.Width - logoBmp.Width - 40, canvas.Height - logoBmp.Height - 40, null);
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
            var logoBmp = BitmapFactory.DecodeResource(Resources, Resource.Drawable.logo100px);

            _compassView.Draw(canvas);
            canvas.DrawBitmap(logoBmp, canvas.Width - logoBmp.Width - 40, canvas.Height - logoBmp.Height - 40, null);

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
            if (_context.Settings.ShowElevationProfile && (_context.ElevationProfileData == null || _context.ElevationProfileDataDistance < _context.Settings.MaxDistance) && _elevationProfileBeingGenerated == false)
            {
                GenerateElevationProfile();
            }
            _displayTerrainButton.SetImageResource(_context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);
        }

        private void GenerateElevationProfile()
        {
            try
            {
                if (photodata.JsonElevationProfileData != null)
                {
                    _context.ElevationProfileData = JsonConvert.DeserializeObject<ElevationProfileData>(photodata.JsonElevationProfileData);
                    if (_context.ElevationProfileData != null)
                    {
                        RefreshElevationProfile();
                        return;
                    }
                } 

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