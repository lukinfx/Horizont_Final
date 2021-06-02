﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Extensions;
using Peaks360App.Tasks;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using Xamarin.Essentials;
using GpsUtils = Peaks360Lib.Utilities.GpsUtils;

namespace Peaks360App.Activities
{
    [Activity(Label = "PhotoImportActivity")]
    public class PhotoImportActivity : Activity, View.IOnClickListener
    {
        public static int REQUEST_IMPORT_IMAGE = Definitions.BaseResultCode.PHOTO_IMPORT_ACTIVITY + 0;

        private IAppContext AppContext { get { return AppContextLiveData.Instance; } }
        private EditText _editTextLatitude;
        private EditText _editTextLongitude;
        private EditText _editTextAltitude;
        private EditText _editTextHeading;
        private EditText _editTextViewAngleHorizontal;
        private EditText _editTextViewAngleVertical;
        private ImageView _imageViewThumbnail;

        private PhotoData _photoData;

        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);
            Platform.Init(this, savedInstanceState);

            if (AppContextLiveData.Instance.IsPortrait)
            {
                SetContentView(Resource.Layout.PhotoImportActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.PhotoImportActivityPortrait);
            }


            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);
            //ActionBar.SetTitle("Photo Import");

            _editTextLatitude = FindViewById<EditText>(Resource.Id.editTextLatitude);
            _editTextLongitude = FindViewById<EditText>(Resource.Id.editTextLongitude);
            _editTextAltitude = FindViewById<EditText>(Resource.Id.editTextAltitude);
            _editTextHeading = FindViewById<EditText>(Resource.Id.editTextHeading);
            _editTextViewAngleHorizontal = FindViewById<EditText>(Resource.Id.editTextViewAngleHorizontal);
            _editTextViewAngleVertical = FindViewById<EditText>(Resource.Id.editTextViewAngleVertical);
            _imageViewThumbnail = FindViewById<ImageView>(Resource.Id.Thumbnail);

            FindViewById<Button>(Resource.Id.buttonBearing).SetOnClickListener(this);
            FindViewById<Button>(Resource.Id.buttonLocation).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.buttonCameraLocationInfo).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.buttonViewDirectionInfo).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.buttonViewAnglesInfo).SetOnClickListener(this);

            _editTextLatitude.TextChanged += OnTextChanged;
            _editTextLongitude.TextChanged += OnTextChanged;
            _editTextAltitude.TextChanged += OnTextChanged;
            _editTextHeading.TextChanged += OnTextChanged;
            _editTextViewAngleVertical.TextChanged += OnTextChanged;
            _editTextViewAngleHorizontal.TextChanged += OnTextChanged;

            long id = Intent.GetLongExtra("Id", -1);
            _photoData = Context.Database.GetPhotoDataItem(id);

            var bmp = BitmapFactory.DecodeByteArray(_photoData.Thumbnail, 0, _photoData.Thumbnail.Length);
            _imageViewThumbnail.SetImageBitmap(bmp);

            UpdateCameraLocation(_photoData.GetPhotoGpsLocation());
            UpdateCameraAltitude(_photoData.GetPhotoGpsLocation());
            UpdateViewAngles(_photoData.GetPhotoGpsLocation());
            UpdateHeading(_photoData.Heading);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            EditText et = sender as EditText;
            if (string.IsNullOrEmpty(e.Text.ToString()))
            {
                et.SetBackgroundResource(Resource.Drawable.bg_edittext_warning);
            }
            else
            {
                et.SetBackgroundResource(Resource.Drawable.bg_edittext);
            }
        }

        private void UpdateCameraLocation(GpsLocation location)
        {
            if (GpsUtils.HasLocation(location))
            {
                _editTextLongitude.Text = $"{location.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{location.Latitude:F7}".Replace(",", ".");
            }
            else
            {
                _editTextLongitude.Text = "";
                _editTextLatitude.Text = "";
            }
        }

        private void UpdateCameraAltitude(GpsLocation location)
        {
            if (GpsUtils.HasAltitude(location))
            {
                _editTextAltitude.Text = $"{location.Altitude:F0}";
            }
            else
            {
                _editTextAltitude.Text = "";
            }
        }

        private void UpdateViewAngles(GpsLocation location)
        {
            _editTextViewAngleHorizontal.Text = $"{_photoData.ViewAngleHorizontal:F1}".Replace(",", ".");
            _editTextViewAngleVertical.Text = $"{_photoData.ViewAngleVertical:F1}".Replace(",", ".");
        }

        private void UpdateHeading(double? heading)
        {
            if (heading.HasValue)
            {
                
                _editTextHeading.Text = $"{GpsUtils.Normalize360(heading.Value):F1}".Replace(",", ".");
            }
            else
            {
                _editTextHeading.Text = "";
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PhotoImportActivityMenu, menu);
            //MenuInflater.Inflate(Resource.Menu.EditActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
                case Resource.Id.menu_save:
                    OnSaveClicked();
                    break;
                case Resource.Id.menu_paste:
                    OnPasteGpsLocation();
                    break;
                case Resource.Id.menu_fetch_altitude:
                    OnUpdateElevation();
                    break;
                case Resource.Id.menu_show_on_map:
                    OnOpenMapClicked();
                    break;
            }
            return base.OnOptionsItemSelected(item);
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

        private void OnCameraDirectionClicked()
        {
            if (!TryGetGpsLocation(out GpsLocation _))
            {
                PopupHelper.ErrorDialog(this, "Set camera location first.");
            }

            Intent intent = new Intent(this, typeof(PoiSelectActivity));
            intent.SetAction(PoiSelectActivity.REQUEST_SELECT_CAMERADIRECTION.ToString());
            StartActivityForResult(intent, PoiSelectActivity.REQUEST_SELECT_CAMERADIRECTION);
        }

        private void OnCameraLocationClicked()
        {
            Intent intent = new Intent(this, typeof(PoiSelectActivity));
            StartActivityForResult(intent, PoiSelectActivity.REQUEST_SELECT_CAMERALOCATION);
        }

        private void OnSaveClicked()
        {
            try
            {
                if (!TryGetGpsLocation(out GpsLocation location))
                {
                    return;
                }
                _photoData.Latitude = location.Latitude;
                _photoData.Longitude = location.Longitude;
                _photoData.Altitude = location.Altitude;


                if (!string.IsNullOrEmpty(_editTextHeading.Text))
                {
                    double heading = 0;
                    if (!double.TryParse(_editTextHeading.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out heading))
                    {
                        PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                        return;
                    }
                    _photoData.Heading = GpsUtils.Normalize180(heading);
                }
                else
                {
                    _photoData.Heading = null;
                }


                if (!double.TryParse(_editTextViewAngleHorizontal.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var viewAngleHorizontal))
                {
                    PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                    return;
                }
                _photoData.ViewAngleHorizontal = viewAngleHorizontal;

                if (!double.TryParse(_editTextViewAngleVertical.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var viewAngleVertical))
                {
                    PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat);
                    return;
                }
                _photoData.ViewAngleVertical = viewAngleVertical;
                Context.PhotosModel.UpdateItem(_photoData);

                Finish();
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Unable to save image. Error details: " + ex.Message);
            }
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
                UpdateCameraLocation(location);

                /*if (string.IsNullOrEmpty(_editTextName.Text))
                {
                    UpdateLocationName(location);
                }*/
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, Resource.String.EditPoi_WrongFormat, ex.Message);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(_editTextAltitude.Text))
                {
                    if (TryGetElevation(location, out var altitude))
                    {
                        location.Altitude = altitude;
                        UpdateCameraAltitude(location);
                    }
                }
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

                    if (TryGetElevation(location, out var altitude))
                    {
                        location.Altitude = altitude;
                        UpdateCameraAltitude(location);
                    }
                    else
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

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonCameraLocationInfo:
                    PopupHelper.InfoDialog(this, Resource.String.PhotoImport_CameraLocationInfo);
                    break;
                case Resource.Id.buttonViewDirectionInfo:
                    PopupHelper.InfoDialog(this, Resource.String.PhotoImport_ViewDirectionInfo);
                    break;
                case Resource.Id.buttonViewAnglesInfo:
                    PopupHelper.InfoDialog(this, Resource.String.PhotoImport_ViewAnglesInfo);
                    break;

                case Resource.Id.buttonLocation:
                    OnCameraLocationClicked();
                    break;
                case Resource.Id.buttonBearing:
                    OnCameraDirectionClicked();
                    break;
            }
        }


        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == PoiSelectActivity.RESULT_OK)
            {
                var id = data.GetLongExtra("Id", 0);

                var selectedPoint = (id == (long)PoiId.CURRENT_LOCATION)
                    ? PoiSelectActivity.GetMyLocationPoi(AppContext)
                    : AppContext.Database.GetItem(id);

                if (requestCode == PoiSelectActivity.REQUEST_SELECT_CAMERADIRECTION)
                {
                    if (!TryGetGpsLocation(out GpsLocation location))
                    {
                        PopupHelper.ErrorDialog(this, "Set camera location first.");
                    }
                    var bearing = GpsUtils.QuickBearing(location, new GpsLocation(selectedPoint.Longitude, selectedPoint.Latitude, selectedPoint.Altitude));
                    UpdateHeading(bearing);
                }
                else if (requestCode == PoiSelectActivity.REQUEST_SELECT_CAMERALOCATION)
                {
                    var location = new GpsLocation(selectedPoint.Longitude, selectedPoint.Latitude, selectedPoint.Altitude);
                    UpdateCameraLocation(location);
                    UpdateCameraAltitude(location);
                }
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

        private bool TryGetElevation(GpsLocation location, out double altitude)
        {
            var et = new ElevationTile(location);
            if (et.Exists())
            {
                if (et.LoadFromZip())
                {
                    altitude = et.GetElevation(location);
                    return true;
                }
            }

            altitude = 0;
            return false;
        }
    }
}
