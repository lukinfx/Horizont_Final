using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Android.Views.View;

using HorizontApp.Activities;
using HorizontApp.DataAccess;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;

using HorizontApp.Utilities;
using HorizontApp.Views.ListOfPoiView;
using HorizontApp.Domain.Enums;
using Xamarin.Essentials;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HorizontApp.Providers;

namespace HorizontApp.Activities
{
    [Activity(Label = "EditActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class EditActivity : Activity, IOnClickListener
    { 
        EditText editTextName;

        EditText editTextLatitude;
        EditText editTextLongitude;
        EditText editTextAltitude;
        Spinner spinnerCategory;
        Button buttonSave;
        Button back;
        Button paste;
        Poi item = new Poi();
        PoiDatabase database;
        long id;
        PoiCategory category;

        private PoiCategory[] _poiCategories = new PoiCategory[] { PoiCategory.Castles, PoiCategory.Churches, PoiCategory.Lakes, PoiCategory.Mountains, PoiCategory.Palaces, PoiCategory.Ruins, PoiCategory.Test, PoiCategory.Transmitters, PoiCategory.ViewTowers};

        public PoiDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new PoiDatabase();
                }
                return database;
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)

        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            id = Intent.GetLongExtra("Id", -1);
            

            SetContentView(Resource.Layout.EditActivity);

            editTextName = FindViewById<EditText>(Resource.Id.editTextName);
            editTextName.SetOnClickListener(this);

            editTextLatitude = FindViewById<EditText>(Resource.Id.editTextLatitude);
            editTextLatitude.SetOnClickListener(this);

            editTextLongitude = FindViewById<EditText>(Resource.Id.editTextLongitude);
            editTextLongitude.SetOnClickListener(this);

            editTextAltitude = FindViewById<EditText>(Resource.Id.editTextAltitude);
            editTextAltitude.SetOnClickListener(this);

            back = FindViewById<Button>(Resource.Id.buttonBack);
            back.SetOnClickListener(this);

            paste = FindViewById<Button>(Resource.Id.buttonPaste);
            paste.SetOnClickListener(this);

            spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _poiCategories.ToList());
            spinnerCategory.Adapter = adapter;
            spinnerCategory.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnerCategory_ItemSelected);

            buttonSave = FindViewById<Button>(Resource.Id.buttonSave);
            buttonSave.SetOnClickListener(this);

            if (id != -1)
            {
                item = Database.GetItem(id);
                spinnerCategory.SetSelection(_poiCategories.ToList().FindIndex(i => i == item.Category));
                editTextName.Text = item.Name;
                editTextAltitude.Text = String.Format("{0,5:#.0}", item.Altitude);
                editTextLongitude.Text = String.Format("{0,3:#.00000000}", item.Longitude);
                editTextLatitude.Text = String.Format("{0,3:#.00000000}", item.Latitude);
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
                editTextAltitude.Text = location.Altitude.ToString();
                editTextLatitude.Text = location.Latitude.ToString();
                editTextLongitude.Text = location.Altitude.ToString();
            }

            

            // Create your application here
        }

        

        private void spinnerCategory_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            category = _poiCategories[e.Position];
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonBack:
                    Finish();
                    break;
                case Resource.Id.buttonSave:
                    item.Name = editTextName.Text;

                    if (_isGPSLocation(editTextLatitude.Text, editTextLongitude.Text, editTextAltitude.Text))
                    {
                        item.Latitude = Convert.ToDouble(editTextLatitude.Text);
                        item.Longitude = Convert.ToDouble(editTextLongitude.Text);
                        item.Altitude = Convert.ToDouble(editTextAltitude.Text);
                        item.Category = category;
                        if (id == -1)
                        {
                            Database.InsertItemAsync(item);
                        }
                        else
                        {
                            database.UpdateItem(item);
                        }
                        Finish();
                    }
                    else
                    {
                        Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                        AlertDialog alert = dialog.Create();
                        alert.SetTitle("Wrong format");
                        alert.SetMessage("Use correct format. Example: '12,34567890' ");
                        alert.SetIcon(Resource.Drawable.notification_bg);
                        alert.SetButton("OK", (c, ev) =>
                        {
                            // Ok button click task  
                        });
                        alert.Show();
                    }
                    break;
                case Resource.Id.buttonPaste:
                    if (Clipboard.HasText)
                    {
                        try
                        {
                            string ClipBoardText = ClipBoardInput().Result;

                            if (ClipBoardText == null) ; //nepodarilo se ziskat text, je ptreba osetrit?

                            GpsLocation location = Parser(ClipBoardText);
                            editTextAltitude.Text = String.Format("{0,5:#}", location.Altitude);
                            editTextLongitude.Text = String.Format("{0,3:#.00000000}", location.Longitude);
                            editTextLatitude.Text = String.Format("{0,3:#.00000000}", location.Latitude);
                        }

                        catch (Exception ex)
                        {
                            PopupDialog("The format is not correct", ex.Message);
                        }
                    }
                    else
                        PopupDialog("Error", "It seems you don't have any text in your ClipBoard.");
                    break;

            }
        }

        private async Task<string> ClipBoardInput()
        {
            string clipboardText = await Clipboard.GetTextAsync();
            return clipboardText;
        }

        private bool _isGPSLocation(string lat, string lon, string alt)
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
                PopupDialog("Error while parsing", ex.Message);
                return null;
            }
        }

        public void PopupDialog(string title, string message)
        {
            using (var dialog = new AlertDialog.Builder(this))
            {
                dialog.SetTitle(title);
                dialog.SetMessage(message);
                dialog.Show();
            }
        }
    }
}