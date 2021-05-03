using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Utilities;
using Peaks360App.Utilities;
using Peaks360App.AppContext;
using Peaks360App.Providers;
using static Android.Views.View;
using Exception = System.Exception;
using String = System.String;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/PoiEditActivity")]
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
        private Spinner _spinnerCountry;
        private Button _buttonOpenMap;
        private Button _buttonOpenWiki;
        private Button _buttonTeleport;
        private ImageView _buttonFavourite;
        private ImageView _thumbnail;
        private Poi _item = new Poi();
        private long _id;
        private bool _isDirty = false;
        private PoiCategory _category;
        private PoiCountry _country;
        private ElevationTile _elevationTile;

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

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);
            ActionBar.SetTitle(Resource.String.PoiEditActivity);

            _editTextName = FindViewById<EditText>(Resource.Id.editTextName);
            _editTextLatitude = FindViewById<EditText>(Resource.Id.editTextLatitude);
            _editTextLongitude = FindViewById<EditText>(Resource.Id.editTextLongitude);
            _editTextAltitude = FindViewById<EditText>(Resource.Id.editTextAltitude);
            _buttonFavourite = FindViewById<ImageView>(Resource.Id.buttonFavourite);
            _buttonOpenWiki = FindViewById<Button>(Resource.Id.buttonWiki);
            _buttonOpenMap = FindViewById<Button>(Resource.Id.buttonMap);
            _buttonTeleport = FindViewById<Button>(Resource.Id.buttonTeleport);
            _spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            _spinnerCountry = FindViewById<Spinner>(Resource.Id.spinnerCountry);

            _spinnerCountry.Adapter = new CountryAdapter(this);

            _spinnerCategory.Adapter = new CategoryAdapter(this);

            _thumbnail = FindViewById<ImageView>(Resource.Id.Thumbnail);
            
            if (_id != -1)
            {
                _item = Context.Database.GetItem(_id);

                var categoryIndex = (_spinnerCategory.Adapter as CategoryAdapter).GetPosition(_item.Category);
                _spinnerCategory.SetSelection(categoryIndex);

                var countryIndex = (_spinnerCountry.Adapter as CountryAdapter).GetPosition(_item.Country);
                _spinnerCountry.SetSelection(countryIndex);
                _editTextName.Text = _item.Name;
                _editTextAltitude.Text = $"{_item.Altitude:F0}";
                _editTextLongitude.Text = $"{_item.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{_item.Latitude:F7}".Replace(",", ".");

                _thumbnail.SetImageResource(PoiCategoryHelper.GetImage(_item.Category));
            }
            else
            {
                var country = PoiCountryHelper.GetDefaultCountry();
                var countryIndex = (_spinnerCountry.Adapter as CountryAdapter).GetPosition(country);
                _spinnerCountry.SetSelection(countryIndex);
            }

            _buttonFavourite.SetImageResource(_item.Favorite ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);

            _buttonOpenWiki.Enabled = (string.IsNullOrEmpty(_item.Wikidata) && string.IsNullOrEmpty(_item.Wikipedia)) ? false : true;
            _buttonOpenWiki.Visibility = (string.IsNullOrEmpty(_item.Wikidata) && string.IsNullOrEmpty(_item.Wikipedia)) ? ViewStates.Gone : ViewStates.Visible;

            //finally set-up event listeners
            _editTextName.TextChanged += OnTextChanged;
            _editTextLatitude.TextChanged += OnTextChanged;
            _editTextLongitude.TextChanged += OnTextChanged;
            _editTextAltitude.TextChanged += OnTextChanged;

            _buttonFavourite.SetOnClickListener(this);
            _buttonOpenMap.SetOnClickListener(this);
            _buttonOpenWiki.SetOnClickListener(this);
            _buttonTeleport.SetOnClickListener(this);

            _spinnerCategory.ItemSelected += OnCategorySelected;
            _spinnerCountry.ItemSelected += OnCountrySelected;
        }

        private void SetDirty()
        {
            _isDirty = true;
        }

        private bool IsDirty()
        {
            return _isDirty || _item.Category != _category;
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _isDirty = false;
        }
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            SetDirty();
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
                    OnBackPressed();
                    break;
                case Resource.Id.menu_save:
                    OnSave();
                    break;
                case Resource.Id.menu_paste:
                    OnPasteGpsLocation();
                    break;
                case Resource.Id.menu_fetch_altitude:
                    OnUpdateElevation();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonFavourite:
                    OnToggleFavouriteClicked();
                    break;
                case Resource.Id.buttonMap:
                    OnOpenMapClicked();
                    break;
                case Resource.Id.buttonWiki:
                    OnOpenWikiClicked(); 
                    break;
                case Resource.Id.buttonTeleport:
                    OnTeleportToPoiClicked();
                    break;
            }
        }

        public override void OnBackPressed()
        {
            if (IsDirty())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) => { SetResult(RESULT_CANCELED); Finish(); });
                alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
                alert.SetMessage(Resources.GetText(Resource.String.Common_DiscardChanges));
                var answer = alert.Show();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private void OnCategorySelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _category = (_spinnerCategory.Adapter as CategoryAdapter)[e.Position] ?? PoiCategory.Other;
            _thumbnail.SetImageResource(PoiCategoryHelper.GetImage(_category));
        }

        private void OnCountrySelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var country = (_spinnerCountry.Adapter as CountryAdapter)[e.Position];
            if (country.HasValue)
            {
                _country = country.Value;
            }
        }

        private void OnSave()
        {
            _item.Name = _editTextName.Text;

            if (!TryGetGpsLocation(out var location))
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                return;
            }

            _item.Latitude = location.Latitude;
            _item.Longitude = location.Longitude;
            _item.Altitude = location.Altitude;
            _item.Category = _category;
            _item.Country = _country;
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

        private void OnPasteGpsLocation()
        {
            GpsLocation location;

            if (!Clipboard.HasText)
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_EmptyClipboard);
                return;
            }

            try
            {
                string ClipBoardText = Clipboard.GetTextAsync().Result;

                if (ClipBoardText == null)
                {//nepodarilo se ziskat text, je potreba osetrit?
                    PopupHelper.ErrorDialog(this, "There are no text data in Clipboard");
                    return;
                }

                location = Peaks360Lib.Utilities.GpsUtils.ParseGPSLocationText(ClipBoardText);
                _editTextAltitude.Text = $"{location.Altitude:F0}";
                _editTextLongitude.Text = $"{location.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{location.Latitude:F7}".Replace(",", ".");

                Task.Run(async () =>
                {
                    var placeInfo = await PlaceNameProvider.AsyncGetPlaceName(location);
                    _editTextName.Text = placeInfo.PlaceName;

                    var countryIndex = (_spinnerCountry.Adapter as CountryAdapter).GetPosition(placeInfo.Country);
                    if (countryIndex >= 0)
                    {
                        _spinnerCountry.SetSelection(countryIndex);
                    }
                });
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat, ex.Message);
                return;
            }

            try
            {
                GetElevation(location);
            }
            catch
            {
                //ignore error since the GetElevation is not a mandatory part of pasting GPS location from clipboard
            }
        }

        private void OnUpdateElevation()
        {
            if (Peaks360Lib.Utilities.GpsUtils.IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
            {
                try
                {
                    if (!TryGetGpsLocation(out var location))
                    {
                        PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                        return;
                    }

                    var ok = GetElevation(location);

                    if (!ok)
                    {
                        PopupHelper.ErrorDialog(this, Resource.String.EditPoi_AltitudeNotUpdated, Resources.GetText(Resource.String.EditPoi_MissingElevationData));
                    }
                }
                catch (Exception ex)
                {
                    PopupHelper.ErrorDialog(this, Resource.String.EditPoi_AltitudeNotUpdated, ex.Message);
                }
            }
        }

        private void OnOpenMapClicked()
        {
            if (!TryGetGpsLocation(out var manualLocation))
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                return;
            }

            MapUtilities.OpenMap(double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture), double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture));
        }

        private void OnOpenWikiClicked()
        {
            WikiUtilities.OpenWiki(_item);
        }

        private void OnTeleportToPoiClicked()
        {
            if (!TryGetGpsLocation(out var manualLocation))
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                return;
            }

            AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);;
            alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
            {
                AppContextLiveData.Instance.Settings.SetManualLocation(manualLocation);
                SetResult(RESULT_OK_AND_CLOSE_PARENT);
                Finish();
            });
            alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
            alert.SetMessage(String.Format(Resources.GetText(Resource.String.EditPoi_TeleportationWarning), _editTextName.Text));
            var answer = alert.Show();
        }

        private void OnToggleFavouriteClicked()
        {
            _item.Favorite = !_item.Favorite;
            _buttonFavourite.SetImageResource(_item.Favorite ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);
            SetDirty();
        }

        private bool GetElevation(GpsLocation location)
        {
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

        private bool TryGetGpsLocation(out GpsLocation location)
        {
            location = new GpsLocation();
            double lat, lon, alt;

            if (string.IsNullOrEmpty(_editTextLongitude.Text) || string.IsNullOrEmpty(_editTextLatitude.Text))
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                return false;
            }

            if (!double.TryParse(_editTextLongitude.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out lon))
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                return false;
            }

            if (!double.TryParse(_editTextLatitude.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out lat))
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                return false;
            }


            if (!string.IsNullOrEmpty(_editTextAltitude.Text))
            {
                if (!double.TryParse(_editTextAltitude.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out alt))
                {
                    PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                    return false;
                }
            }
            else
            {
                alt = 0;
            }

            location = new GpsLocation(lon, lat, alt);
            return true;
        }
    }
}