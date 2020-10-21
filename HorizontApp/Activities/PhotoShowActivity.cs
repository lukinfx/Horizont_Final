using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using HorizontApp.Utilities;
using HorizontApp.Views;
using HorizontLib.Domain.Models;
using Xamarin.Essentials;
using Xamarin.Forms.Platform.Android;

namespace HorizontApp.Activities
{
    [Activity(Label = "PhotoShowActivity")]
    public class PhotoShowActivity : Activity, GestureDetector.IOnGestureListener
    {
        private static string TAG = "Horizon-PhotoShowActivity";

        private IAppContext _context;
        private TextView _GPSTextView;
        private TextView _headingTextView;
        private ImageView photoView;
        private CompassView _compassView;
        private byte[] _thumbnail;
        private GestureDetector _gestureDetector;
        private Timer _refreshTimer = new Timer();

        private TextView _filterText;

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
            _context.Settings.ViewAngleVertical = (float)photodata.ViewAngleVertical;
            _context.Settings.ViewAngleHorizontal = (float)photodata.ViewAngleHorizontal;


            _filterText = FindViewById<TextView>(Resource.Id.textView1);

            _headingTextView = FindViewById<TextView>(Resource.Id.editText1);
            _GPSTextView = FindViewById<TextView>(Resource.Id.editText2);

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = 10;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;
            _heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            _heightSeekBar.Progress = 0;
            _heightSeekBar.ProgressChanged += OnMinAltitudeChanged;


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
            if (e1.RawY < Resources.DisplayMetrics.HeightPixels / 2)
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
    }
}