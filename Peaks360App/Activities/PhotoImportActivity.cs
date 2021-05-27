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
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
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

            long id = Intent.GetLongExtra("Id", -1);
            _photoData = Context.Database.GetPhotoDataItem(id);

            var bmp = BitmapFactory.DecodeByteArray(_photoData.Thumbnail, 0, _photoData.Thumbnail.Length);
            _imageViewThumbnail.SetImageBitmap(bmp);

            _editTextViewAngleHorizontal.Text = $"{_photoData.ViewAngleHorizontal:F1}";
            _editTextViewAngleVertical.Text = $"{_photoData.ViewAngleVertical:F1}";
            _editTextLongitude.Text = $"{_photoData.Longitude:F7}".Replace(",", ".");
            _editTextLatitude.Text = $"{_photoData.Latitude:F7}".Replace(",", ".");
            _editTextAltitude.Text = $"{_photoData.Altitude:F0}";

            _editTextHeading.Text = _photoData.Heading.HasValue ? $"{_photoData.Heading.Value:F1}" : "";
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.PhotoImportActivityMenu, menu);
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
                /*case Resource.Id.menu_show_on_map:
                    OnOpenMapClicked();
                    break;*/
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

        private void OnBearingClicked()
        {
            Intent intent = new Intent(this, typeof(PoiSelectActivity));
            StartActivityForResult(intent, PoiSelectActivity.REQUEST_SELECT_POI);
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
                    _photoData.Heading = heading;
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

                Context.Database.UpdateItem(_photoData);

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
                _editTextLongitude.Text = $"{location.Longitude:F7}".Replace(",", ".");
                _editTextLatitude.Text = $"{location.Latitude:F7}".Replace(",", ".");

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
                        _editTextAltitude.Text = $"{altitude:F0}";
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
                        _editTextAltitude.Text = $"{altitude:F0}";
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
                case Resource.Id.buttonLocation:
                    //###
                    break;
                case Resource.Id.buttonBearing:
                    OnBearingClicked();
                    break;
            }
        }


        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PoiSelectActivity.REQUEST_SELECT_POI)
            {
                if (resultCode == PoiSelectActivity.RESULT_OK)
                {
                    var id = data.GetLongExtra("Id", 0);

                    var selectedPoint = (id == (long)PoiId.CURRENT_LOCATION)
                        ? PoiSelectActivity.GetMyLocationPoi(AppContext)
                        : AppContext.Database.GetItem(id);

                    if (TryGetGpsLocation(out GpsLocation location))
                    {
                        var bearing = GpsUtils.QuickBearing(location, new GpsLocation(selectedPoint.Longitude, selectedPoint.Latitude, selectedPoint.Altitude));
                        _editTextHeading.Text = $"{bearing:F1}";
                    }
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
