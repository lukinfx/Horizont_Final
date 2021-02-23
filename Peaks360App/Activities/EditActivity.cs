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
using static Android.Views.View;
using Xamarin.Essentials;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Enums;
using Peaks360App.AppContext;
using Peaks360App.Providers;
using Peaks360Lib.Utilities;
using GpsUtils = Peaks360App.Utilities.GpsUtils;

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
        private Button _buttonOpenMap;
        private Button _buttonOpenWiki;
        private Button _buttonTeleport;
        private ImageView _buttonFavourite;
        private ImageView _thumbnail;
        private ImageButton _buttonPaste;
        private Poi _item = new Poi();
        private long _id;
        private bool _isDirty = false;
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
            
            var categoryList = new List<string>();
            categoryList.Add(Resources.GetText(Resource.String.Category_Mountains));
            categoryList.Add(Resources.GetText(Resource.String.Category_Cities));
            categoryList.Add(Resources.GetText(Resource.String.Category_Historic));
            categoryList.Add(Resources.GetText(Resource.String.Category_Churches));
            categoryList.Add(Resources.GetText(Resource.String.Category_ViewTowers));
            categoryList.Add(Resources.GetText(Resource.String.Category_Transmitters));
            categoryList.Add(Resources.GetText(Resource.String.Category_Lakes));
            categoryList.Add(Resources.GetText(Resource.String.Category_Other));
            _spinnerCategory.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, categoryList);

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

            _buttonFavourite.SetImageResource(_item.Favorite ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);

            _buttonOpenWiki.Enabled = (string.IsNullOrEmpty(_item.Wikidata) && string.IsNullOrEmpty(_item.Wikipedia)) ? false : true;
            _buttonOpenWiki.Visibility = (string.IsNullOrEmpty(_item.Wikidata) && string.IsNullOrEmpty(_item.Wikipedia)) ? ViewStates.Gone : ViewStates.Visible;

            //finally set-up event listeners
            _editTextName.TextChanged += OnTextChanged;
            _editTextLatitude.TextChanged += OnTextChanged;
            _editTextLongitude.TextChanged += OnTextChanged;
            _editTextAltitude.TextChanged += OnTextChanged;

            _editTextName.SetOnClickListener(this);
            _editTextLatitude.SetOnClickListener(this);
            _editTextLongitude.SetOnClickListener(this);
            _editTextAltitude.SetOnClickListener(this);

            _buttonFavourite.SetOnClickListener(this);
            _buttonOpenMap.SetOnClickListener(this);
            _buttonOpenWiki.SetOnClickListener(this);
            _buttonTeleport.SetOnClickListener(this);

            _spinnerCategory.ItemSelected += OnSpinnerCategoryItemSelected;
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
                case Resource.Id.buttonFavourite:
                    ToggleFavourite();
                    break;
                case Resource.Id.buttonMap:
                    MapUtilities.OpenMap(double.Parse(_editTextLatitude.Text, CultureInfo.InvariantCulture), double.Parse(_editTextLongitude.Text, CultureInfo.InvariantCulture));
                    break;
                case Resource.Id.buttonWiki:
                    WikiUtilities.OpenWiki(_item);
                    break;
                case Resource.Id.buttonTeleport:
                    TeleportToPoi();
                    break;
            }
        }

        public override void OnBackPressed()
        {
            if (IsDirty())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) => { SetResult(RESULT_CANCELED); Finish(); });
                alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) => { });
                alert.SetMessage(Resources.GetText(Resource.String.DiscardChanges));
                var answer = alert.Show();
            }
            else
            {
                base.OnBackPressed();
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

            if (Peaks360Lib.Utilities.GpsUtils.IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
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
                PopupHelper.ErrorDialog(this, Resources.GetText(Resource.String.Error),
                    Resources.GetText(Resource.String.EditPoi_WrongFormat));
            }
        }

        private void PasteGpsLocation()
        {
            if (!Clipboard.HasText)
            {
                PopupHelper.ErrorDialog(this, Resources.GetText(Resource.String.Error),
                    Resources.GetText(Resource.String.EditPoi_EmptyClipboard));
                return;
            }

            try
            {
                string ClipBoardText = GetClipBoardInput().Result;

                if (ClipBoardText == null) ; //nepodarilo se ziskat text, je potreba osetrit?

                GpsLocation location = Peaks360Lib.Utilities.GpsUtils.ParseGPSLocationText(ClipBoardText);
                _editTextAltitude.Text = $"{location.Altitude:F0}";
                _editTextLongitude.Text = $"{location.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{location.Latitude:F7}".Replace(",", ".");

                Task.Run(async () =>
                {
                    var placeName = await PlaceNameProvider.AsyncGetPlaceName(location);
                    _editTextName.Text = placeName;
                });
                
                
            }

            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, Resources.GetText(Resource.String.Error), 
                    Resources.GetText(Resource.String.EditPoi_WrongFormat) + " " + ex.Message);
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
            if (Peaks360Lib.Utilities.GpsUtils.IsGPSLocation(_editTextLatitude.Text, _editTextLongitude.Text, _editTextAltitude.Text))
            {
                try
                {
                    var ok = GetElevation();

                    if (!ok)
                    {
                        PopupHelper.ErrorDialog(this, Resources.GetText(Resource.String.Error),
                            Resources.GetText(Resource.String.EditPoi_AltitudeNotUpdated) + " " + Resources.GetText(Resource.String.EditPoi_MissingElevationData));
                    }
                }
                catch (Exception ex)
                {
                    PopupHelper.ErrorDialog(this, Resources.GetText(Resource.String.Error),
                        Resources.GetText(Resource.String.EditPoi_AltitudeNotUpdated) + " " + ex.Message);
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
            alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) =>
            {
                AppContextLiveData.Instance.Settings.ManualLocation = manualLocation;
                AppContextLiveData.Instance.Settings.IsManualLocation = true;
                SetResult(RESULT_OK_AND_CLOSE_PARENT);
                Finish();
            });
            alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) => { });
            alert.SetMessage(String.Format(Resources.GetText(Resource.String.EditPoi_TeleportationWarning), _editTextName.Text));
            var answer = alert.Show();
        }

        private void ToggleFavourite()
        {
            _item.Favorite = !_item.Favorite;
            _buttonFavourite.SetImageResource(_item.Favorite ? Resource.Drawable.f_heart_solid : Resource.Drawable.f_heart_empty);
            SetDirty();
        }

        private async Task<string> GetClipBoardInput()
        {
            string clipboardText = await Clipboard.GetTextAsync();
            return clipboardText;
        }

        private bool GetElevation()
        {
            if (!double.TryParse(_editTextLongitude.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var lon))
            {
                return false;
            }

            if (!double.TryParse(_editTextLatitude.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var lat))
            {
                return false;
            }
            
            var location = new GpsLocation(lon, lat, 0);

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