using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using static Android.Views.View;
using Xamarin.Essentials;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontLib.Domain.Enums;
using HorizontApp.AppContext;
using HorizontLib.Utilities;
using GpsUtils = HorizontApp.Utilities.GpsUtils;

namespace HorizontApp.Activities
{
    [Activity(Label = "EditActivity")]
    public class EditActivity : Activity, IOnClickListener
    {
        public static int REQUEST_ADD_POI = Definitions.BaseResultCode.POIEDIT_ACTIVITY + 0;
        public static int REQUEST_EDIT_POI = Definitions.BaseResultCode.POIEDIT_ACTIVITY + 1;

        public static Result RESULT_CANCELED { get { return Result.Canceled; } }
        public static Result RESULT_OK { get { return Result.Ok; } }
        public static Result RESULT_OK_AND_CLOSE_PARENT { get { return (Result)2; } }

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
        private ElevationTile _elevationTile;

        private PoiCategory[] _poiCategories = new PoiCategory[] { PoiCategory.Mountains, PoiCategory.Cities, PoiCategory.Historic, PoiCategory.Churches, PoiCategory.Lakes, PoiCategory.Transmitters, PoiCategory.ViewTowers, PoiCategory.Other};

        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);
            Platform.Init(this, savedInstanceState);

            if (AppContextLiveData.Instance.IsPortrait)
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
                    SetResult(RESULT_CANCELED);
                    Finish();
                    break;
                case Resource.Id.menu_save:
                    Save();
                    break;
                case Resource.Id.menu_paste:
                    PasteGpsLocation();
                    break;

                case Resource.Id.menu_fetch_altitude:
                    UpdateElevation();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonMap:
                    MapUtilities.OpenMap(double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture), double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture));
                    break;
                case Resource.Id.buttonWiki:
                    WikiUtilities.OpenWiki(_item);
                    break;
                case Resource.Id.buttonTeleport:
                {
                    TeleportToPoi();
                    break;
                }
            }
        }

        private void OnSpinnerCategoryItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _category = _poiCategories[e.Position];
            _thumbnail.SetImageResource(PoiCategoryHelper.GetImage(_category));
        }

        private void Save()
        {
            _item.Name = _editTextName.Text;

            if (HorizontLib.Utilities.GpsUtils.IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
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

                var resultIntent = new Intent();
                resultIntent.PutExtra("Id", _item.Id);
                SetResult(RESULT_OK, resultIntent);
                Finish();
            }
            else
            {
                PopupHelper.ErrorDialog(this, "Wrong format", "Use correct format. Example: '12,34567890' ");
            }
        }

        private void PasteGpsLocation()
        {
            if (!Clipboard.HasText)
            {
                PopupHelper.ErrorDialog(this, "Error", "It seems you don't have any text in your ClipBoard.");
                return;
            }

            try
            {
                string ClipBoardText = GetClipBoardInput().Result;

                if (ClipBoardText == null) ; //nepodarilo se ziskat text, je potreba osetrit?

                GpsLocation location = HorizontLib.Utilities.GpsUtils.ParseGPSLocationText(ClipBoardText);
                _editTextAltitude.Text = $"{location.Altitude:F0}";
                _editTextLongitude.Text = $"{location.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{location.Latitude:F7}".Replace(",", ".");

                Task.Run(async () =>
                {
                    var placeName = await GpsUtils.AsyncGetPlaceName(location);
                    _editTextName.Text = placeName;
                });
                
                
            }

            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "The format is not correct", ex.Message);
            }

            try
            {
                GetElevation();
            }
            catch
            {
                //ignore error since the GetElevation is not a mandatory part of pasting GPS location from clipboard
            }
        }

        private void UpdateElevation()
        {
            if (HorizontLib.Utilities.GpsUtils.IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
            {
                try
                {
                    var ok = GetElevation();

                    if (!ok)
                    {
                        PopupHelper.ErrorDialog(this, "Error", "The elevation was not updated. Missing elevation data.");
                    }
                }
                catch (Exception ex)
                {
                    PopupHelper.ErrorDialog(this, "Error", "The elevation was not updated. " + ex.Message);
                }
            }
        }
        private void TeleportToPoi()
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
                AppContextLiveData.Instance.Settings.IsManualLocation = true;
                SetResult(RESULT_OK_AND_CLOSE_PARENT);
                Finish();
            });
            alert.SetNegativeButton("No", (senderAlert, args) => { });
            alert.SetMessage($"Your location will be set to { _editTextName.Text}. To reset your location, press [Reset] button in camera view or reset manual location in application settings.\r\n\r\nDo you want to continue?");
            var answer = alert.Show();
        }


        private async Task<string> GetClipBoardInput()
        {
            string clipboardText = await Clipboard.GetTextAsync();
            return clipboardText;
        }

        private bool GetElevation()
        {
            var location = new GpsLocation(
                double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture),
                double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture),
                0);

            if (_elevationTile == null || !_elevationTile.HasElevation(location))
            {
                _elevationTile = null;
                var et = new ElevationTile(location);
                if (et.Exists())
                {
                    if (et.LoadFromZip())
                    {
                        _elevationTile = et;
                    }
                }
            }

            if (_elevationTile != null)
            {
                var altitude = _elevationTile.GetElevation(location);
                _editTextAltitude.Text = $"{altitude:F0}";
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}