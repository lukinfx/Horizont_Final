using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using static Android.Views.View;
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontLib.Domain.Enums;
using Xamarin.Essentials;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HorizontApp.Providers;

namespace HorizontApp.Activities
{
    [Activity(Label = "EditActivity")]
    public class EditActivity : Activity, IOnClickListener
    { 
        private EditText _editTextName;
        private EditText _editTextLatitude;
        private EditText _editTextLongitude;
        private EditText _editTextAltitude;
        private Spinner _spinnerCategory;
        private Button _buttonSave;
        private Button _buttonBack;
        private Button _buttonPaste;
        private Poi _item = new Poi();
        private PoiDatabase _database;
        private long _id;
        private PoiCategory _category;

        private PoiCategory[] _poiCategories = new PoiCategory[] { PoiCategory.Castles, PoiCategory.Churches, PoiCategory.Lakes, PoiCategory.Mountains, PoiCategory.Palaces, PoiCategory.Ruins, PoiCategory.Test, PoiCategory.Transmitters, PoiCategory.ViewTowers};

        public PoiDatabase Database
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
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            var orientation = mainDisplayInfo.Orientation;
            if (orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.EditActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.EditActivityLandscape);
            }

            _id = Intent.GetLongExtra("Id", -1);

            _editTextName = FindViewById<EditText>(Resource.Id.editTextName);
            _editTextName.SetOnClickListener(this);

            _editTextLatitude = FindViewById<EditText>(Resource.Id.editTextLatitude);
            _editTextLatitude.SetOnClickListener(this);

            _editTextLongitude = FindViewById<EditText>(Resource.Id.editTextLongitude);
            _editTextLongitude.SetOnClickListener(this);

            _editTextAltitude = FindViewById<EditText>(Resource.Id.editTextAltitude);
            _editTextAltitude.SetOnClickListener(this);

            _buttonBack = FindViewById<Button>(Resource.Id.buttonBack);
            _buttonBack.SetOnClickListener(this);

            _buttonPaste = FindViewById<Button>(Resource.Id.buttonPaste);
            _buttonPaste.SetOnClickListener(this);

            _spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _poiCategories.ToList());
            _spinnerCategory.Adapter = adapter;
            _spinnerCategory.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnerCategory_ItemSelected);

            _buttonSave = FindViewById<Button>(Resource.Id.buttonSave);
            _buttonSave.SetOnClickListener(this);

            if (_id != -1)
            {
                _item = Database.GetItem(_id);
                _spinnerCategory.SetSelection(_poiCategories.ToList().FindIndex(i => i == _item.Category));
                _editTextName.Text = _item.Name;
                _editTextAltitude.Text = String.Format("{0,5:#.0}", _item.Altitude);
                _editTextLongitude.Text = String.Format("{0,3:#.00000000}", _item.Longitude);
                _editTextLatitude.Text = String.Format("{0,3:#.00000000}", _item.Latitude);
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

                //nasledujici kod zatim nefunguje
                GpsLocationProvider gpsLocationProvider = new GpsLocationProvider();
                GpsLocation location = gpsLocationProvider.CurrentLocation;
                _editTextAltitude.Text = location.Altitude.ToString();
                _editTextLatitude.Text = location.Latitude.ToString();
                _editTextLongitude.Text = location.Altitude.ToString();
            }

            

            // Create your application here
        }

        

        private void spinnerCategory_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _category = _poiCategories[e.Position];
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
                case Resource.Id.buttonSave:
                    _item.Name = _editTextName.Text;

                    if (IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
                    {
                        _item.Latitude = Convert.ToDouble(_editTextLatitude.Text);
                        _item.Longitude = Convert.ToDouble(_editTextLongitude.Text);
                        _item.Altitude = Convert.ToDouble(_editTextAltitude.Text);
                        _item.Category = _category;
                        if (_id == -1)
                        {
                            Database.InsertItemAsync(_item);
                        }
                        else
                        {
                            _database.UpdateItem(_item);
                        }
                        Finish();
                    }
                    else
                    {
                        PopupHelper.ErrorDialog(this,"Wrong format", "Use correct format. Example: '12,34567890' ");
                    }
                    break;
                case Resource.Id.buttonPaste:
                    if (Clipboard.HasText)
                    {
                        try
                        {
                            string ClipBoardText = GetClipBoardInput().Result;

                            if (ClipBoardText == null) ; //nepodarilo se ziskat text, je ptreba osetrit?

                            GpsLocation location = Parser(ClipBoardText);
                            _editTextAltitude.Text = String.Format("{0,5:#}", location.Altitude);
                            _editTextLongitude.Text = String.Format("{0,3:#.00000000}", location.Longitude);
                            _editTextLatitude.Text = String.Format("{0,3:#.00000000}", location.Latitude);
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