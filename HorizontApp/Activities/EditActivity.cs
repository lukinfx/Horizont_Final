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
        Poi item;
        PoiDatabase database;

        private PoiCategory[] _poiCategories = new PoiCategory[] { PoiCategory.Castles, PoiCategory.Churches, PoiCategory.Lakes, PoiCategory.Mountains, PoiCategory.Palaces, PoiCategory.Peaks, PoiCategory.Ruins, PoiCategory.Test, PoiCategory.Transmitters, PoiCategory.ViewTowers};

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

            long id = Intent.GetLongExtra("Id", -1);
            item = Database.GetItem(id);

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

            spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, _poiCategories.ToList());
            spinnerCategory.Adapter = adapter;
            spinnerCategory.SetSelection(_poiCategories.ToList().FindIndex(i => i == item.Category));
            spinnerCategory.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinnerCategory_ItemSelected);
            buttonSave = FindViewById<Button>(Resource.Id.buttonSave);
            buttonSave.SetOnClickListener(this);


            editTextName.Text = item.Name;
            editTextAltitude.Text = String.Format("{0,5:#.0}", item.Altitude);
            editTextLongitude.Text = String.Format("{0,3:#.00000000}", item.Longitude);
            editTextLatitude.Text = String.Format("{0,3:#.00000000}", item.Latitude);

            // Create your application here
        }

        private void spinnerCategory_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            item.Category = _poiCategories[e.Position];
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
                    item.Latitude = Convert.ToDouble(editTextLatitude.Text);
                    item.Longitude = Convert.ToDouble(editTextLongitude.Text);
                    item.Altitude = Convert.ToDouble(editTextAltitude.Text);
                    database.UpdateItem(item);
                    Finish();
                    break;
            }
        }
    }
}