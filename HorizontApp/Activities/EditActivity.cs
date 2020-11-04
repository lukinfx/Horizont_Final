using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using static Android.Views.View;
using Xamarin.Essentials;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontLib.Domain.Enums;
using HorizontApp.AppContext;

namespace HorizontApp.Activities
{
    [Activity(Label = "EditActivity")]
    public class EditActivity : Activity, IOnClickListener
    {
        public static int REQUEST_ADD_POI = 0;
        public static int REQUEST_EDIT_POI = 1;
        

        private EditText _editTextName;
        private EditText _editTextLatitude;
        private EditText _editTextLongitude;
        private EditText _editTextAltitude;
        private Spinner _spinnerCategory;
        private ImageButton _buttonOpenMap;
        private ImageButton _buttonOpenWiki;
        private ImageButton _buttonTeleport;
        private ImageView _thumbnail;
        private ImageButton _buttonPaste;
        private Poi _item = new Poi();
        private long _id;
        private PoiCategory _category;

        private PoiCategory[] _poiCategories = new PoiCategory[] { PoiCategory.Castles, PoiCategory.Churches, PoiCategory.Lakes, PoiCategory.Mountains, PoiCategory.Palaces, PoiCategory.Ruins, PoiCategory.Transmitters, PoiCategory.ViewTowers};

        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.EditActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.EditActivityLandscape);
            }

            _id = Intent.GetLongExtra("Id", -1);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            //ActionBar.Title = "Edit POI";
            //ActionBar.SetHomeButtonEnabled(true);
            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            _editTextName = FindViewById<EditText>(Resource.Id.editTextName);
            _editTextName.SetOnClickListener(this);

            _editTextLatitude = FindViewById<EditText>(Resource.Id.editTextLatitude);
            _editTextLatitude.SetOnClickListener(this);

            _editTextLongitude = FindViewById<EditText>(Resource.Id.editTextLongitude);
            _editTextLongitude.SetOnClickListener(this);

            _editTextAltitude = FindViewById<EditText>(Resource.Id.editTextAltitude);
            _editTextAltitude.SetOnClickListener(this);

            _buttonOpenMap = FindViewById<ImageButton>(Resource.Id.buttonMap);
            _buttonOpenMap.SetOnClickListener(this);

            _buttonOpenWiki = FindViewById<ImageButton>(Resource.Id.buttonWiki);
            _buttonOpenWiki.SetOnClickListener(this);

            _buttonTeleport = FindViewById<ImageButton>(Resource.Id.buttonTeleport);
            _buttonTeleport.SetOnClickListener(this);


            _spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            _spinnerCategory.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _poiCategories.ToList());
            _spinnerCategory.ItemSelected += OnSpinnerCategoryItemSelected;

            _thumbnail = FindViewById<ImageView>(Resource.Id.Thumbnail);
            
            if (_id != -1)
            {
                _item = Context.Database.GetItem(_id);
                _spinnerCategory.SetSelection(_poiCategories.ToList().FindIndex(i => i == _item.Category));
                _editTextName.Text = _item.Name;
                _editTextAltitude.Text = $"{_item.Altitude:F0}";
                _editTextLongitude.Text = $"{_item.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{_item.Latitude:F7}".Replace(",", ".");

                _thumbnail.SetImageResource(PoiCategoryHelper.GetImage(_item.Category));
            }
            else 
            {
                //MainThread.BeginInvokeOnMainThread(() =>
                //{
                //    if (Clipboard.HasText)
                //    {
                //        ClipBoardInput();
                //    }
                //});
            }


            if (_item.Wikidata == null && _item.Wikipedia == null)
            {
                _buttonOpenWiki.Enabled = false;
                _buttonOpenWiki.Visibility = ViewStates.Gone;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.EditActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    SetResult(Result.Ok);
                    Finish();
                    break;
                case Resource.Id.menu_save:
                    _item.Name = _editTextName.Text;

                    if (IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
                    {
                        _item.Latitude = double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture);
                        _item.Longitude = double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture);
                        _item.Altitude = double.Parse(_editTextAltitude.Text, CultureInfo.InvariantCulture);
                        _item.Category = _category;
                        if (_id == -1)
                        {
                            Context.Database.InsertItemAsync(_item);
                        }
                        else
                        {
                            Context.Database.UpdateItem(_item);
                        }

                        SetResult(Result.Ok);
                        Finish();
                    }
                    else
                    {
                        PopupHelper.ErrorDialog(this, "Wrong format", "Use correct format. Example: '12,34567890' ");
                    }

                    break;
                case Resource.Id.menu_paste:
                    if (Clipboard.HasText)
                    {
                        try
                        {
                            string ClipBoardText = GetClipBoardInput().Result;

                            if (ClipBoardText == null) ; //nepodarilo se ziskat text, je potreba osetrit?

                            GpsLocation location = Parser(ClipBoardText);
                            _editTextAltitude.Text = $"{location.Altitude:F0}";
                            _editTextLongitude.Text = $"{location.Longitude:F7}".Replace(",", ".");
                            _editTextLatitude.Text = $"{location.Latitude:F7}".Replace(",", ".");
                        }

                        catch (Exception ex)
                        {
                            PopupHelper.ErrorDialog(this, "The format is not correct", ex.Message);
                        }
                    }
                    else
                        PopupHelper.ErrorDialog(this, "Error", "It seems you don't have any text in your ClipBoard.");

                    break;
            }

            return base.OnOptionsItemSelected(item);
        }
    
        private void OnSpinnerCategoryItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _category = _poiCategories[e.Position];
            _thumbnail.SetImageResource(PoiCategoryHelper.GetImage(_category));
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonMap:
                    {
                        var location = new Location(double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture), double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture));
                        Map.OpenAsync(location);
                        break;
                    }
                case Resource.Id.buttonWiki:
                    {
                        if (_item.Wikipedia != "")
                        {
                            Browser.OpenAsync("https://en.wikipedia.org/w/index.php?search=" + _item.Wikipedia, BrowserLaunchMode.SystemPreferred);
                        }
                        else
                        {
                            Browser.OpenAsync("https://www.wikidata.org/wiki/" + _item.Wikidata, BrowserLaunchMode.SystemPreferred);
                        }

                        break;
                    }
                case Resource.Id.buttonTeleport:
                {
                    var manualLocation = new GpsLocation()
                    {
                        Latitude = double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture),
                        Longitude = double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture),
                        Altitude = double.Parse(_editTextAltitude.Text, CultureInfo.InvariantCulture)
                    };

                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                    alert.SetPositiveButton("Yes", (senderAlert, args) =>
                    {
                        AppContextLiveData.Instance.Settings.ManualLocation = manualLocation;

                        SetResult(Result.Canceled);
                        Finish();
                    });
                    alert.SetNegativeButton("No", (senderAlert, args) => { });
                    alert.SetMessage($"Your location will be set to { _editTextName.Text}. To reset your location, press [Reset] button in camera view or reset manual location in application settings.\r\n\r\nDo you want to continue?");
                    var answer = alert.Show();

                    break;
                }
            }
        }

        private async Task<string> GetClipBoardInput()
        {
            string clipboardText = await Clipboard.GetTextAsync();
            return clipboardText;
        }

        private bool IsGPSLocation(string lat, string lon, string alt)
        {
            if (Regex.IsMatch(lat, @"[0-9]{0,3}\,[0-9]{0,9}")
                && Regex.IsMatch(lon, @"[0-9]{0,3}\,[0-9]{0,9}")
                && Regex.IsMatch(alt, @"[0-9]{0,5}"))
                { return true; }
            else return false;
        }

        private GpsLocation Parser (string text)
        {
            try
            {
                GpsLocation poi = new GpsLocation();
                text = text.Replace('.', ',');
                int SNchanger = 1;
                int WEchanger = 1;
                if (text.Contains("S"))
                {
                    SNchanger = -1;
                    text = text.Replace("S", "");
                }


                if (text.Contains("W"))
                {
                    WEchanger = -1;
                    text = text.Replace("W", "");
                }

                text = text.Replace("E", "");
                text = text.Replace("N", "");

                string[] location = text.Split(", ");

                poi.Latitude = SNchanger * Convert.ToDouble(location[0]);
                poi.Longitude = WEchanger * Convert.ToDouble(location[1]);
                poi.Altitude = 0;
                return poi;
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error while parsing", ex.Message);
                return null;
            }
        }
    }
}