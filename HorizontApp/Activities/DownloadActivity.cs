using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using HorizontApp.DataAccess;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;
using HorizontApp.Providers;
using HorizontApp.Utilities;
using Newtonsoft.Json;
using Xamarin.Essentials;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "DownloadActivity")]
    public class DownloadActivity : Activity
    {
        private static readonly string WebsiteUrl = "http://krvaveoleje.cz/horizont/";
        private static readonly string IndexFile = "poi-index.json";

        private ListView _downloadItemListView;
        private ListView _downloadCountryListView;
        private Spinner _downloadCountrySpinner;
        private DownloadItemAdapter _downloadItemAdapter;

        private List<PoisToDownload> _downloadItems;
        private List<PoisToDownload> _items;
        private List<PoiCountry> _countries;

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

            InitializeData();

            InitializeUI();
        }

        private void InitializeData()
        {
            //fetch list of already downladed items from database
            var downloadedTask = Database.GetDownloadedPoisAsync();
            downloadedTask.Wait();
            _downloadItems = downloadedTask.Result.ToList();

            //fetch list of item from internet
            var json = GpxFileProvider.GetFile(GetUrl(IndexFile));
            var itemsToDownload = JsonConvert.DeserializeObject<List<PoisToDownload>>(json);

            //combine those two lists together
            foreach (var item in itemsToDownload)
            {
                if (!_downloadItems.Any(x => x.Id == item.Id))
                {
                    _downloadItems.Add(item);
                }
            }

            _countries = _downloadItems.Select(x => x.Country).Distinct().ToList();
            _items = new List<PoisToDownload>();
        }

        private void InitializeUI()
        {
            var countryAdapter = new DownloadCountryAdapter(this, _countries);
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.DownloadActivityPortrait);
                
                _downloadCountrySpinner = FindViewById<Spinner>(Resource.Id.DownloadCountrySpinner);
                _downloadCountrySpinner.Adapter = countryAdapter;
                _downloadCountrySpinner.ItemSelected += OnCountrySpinnerItemSelected;

            }
            else
            {
                SetContentView(Resource.Layout.DownloadActivityLandscape);
                
                _downloadCountryListView = FindViewById<ListView>(Resource.Id.DownloadCountryListView);
                _downloadCountryListView.Adapter = countryAdapter;
                _downloadCountryListView.ItemClick += OnCountryListItemClicked;
            }

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.Title = "Download POIs";
            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            _downloadItemListView = FindViewById<ListView>(Resource.Id.DownloadItemListView);

            _downloadItemAdapter = new DownloadItemAdapter(this);
            _downloadItemListView.Adapter = _downloadItemAdapter;
            _downloadItemListView.ItemClick += OnDownloadListItemClicked;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.DownloadActivityMenu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void OnDownloadListItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            PoisToDownload item = _items[e.Position];
            if (item.DownloadDate == null)
            {
                DownloadFromInternet(item);
            }
            else
            {
                DeleteFromInternet(item);
            }
            _downloadItemAdapter.NotifyDataSetChanged();
        }

        private void OnCountryListItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            OnCountrySelected(e.Position);
        }
        
        private void OnCountrySpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            OnCountrySelected(e.Position);
        }

        private void OnCountrySelected(int position)
        {
            PoiCountry country = _countries[position];
            _items = _downloadItems.Where(x => x.Country == country).ToList();
            _downloadItemAdapter.SetItems(_items);
        }

        private void DownloadFromInternet(PoisToDownload source)
        {
            try
            {
                var file = GpxFileProvider.GetFile(GetUrl(source.Url));
                var listOfPoi = GpxFileParser.Parse(file, source.Category, source.Id);
                Database.InsertAll(listOfPoi);

                source.DownloadDate = DateTime.Now;
                Database.InsertItem(source);

                PopupHelper.InfoDialog(this, "Information", $"{listOfPoi.Count()} items loaded to database.");
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when loading data. {ex.Message}");
            }
        }

        private void DeleteFromInternet(PoisToDownload source)
        {
            try
            {
                Database.DeleteAllFromSource(source.Id);
                
                source.DownloadDate = null;
                Database.DeleteItem(source);

                PopupHelper.InfoDialog(this, "Information", $"Items removed from database.");
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when removing data. {ex.Message}");
            }
        }

        private string GetUrl(string path)
        {
            return WebsiteUrl + path;
        }
    }
}