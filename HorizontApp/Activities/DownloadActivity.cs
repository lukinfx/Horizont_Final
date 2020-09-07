using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using HorizontApp.DataAccess;
using HorizontApp.Domain.Enums;
using HorizontApp.Domain.Models;
using HorizontApp.Providers;
using HorizontApp.Utilities;
using Newtonsoft.Json;
using static Android.Views.View;

namespace HorizontApp.Activities
{
    [Activity(Label = "DownloadActivity", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class DownloadActivity : Activity, IOnClickListener
    {
        private static readonly string WebsiteUrl = "http://krvaveoleje.cz/horizont/";
        private static readonly string IndexFile = "poi-index.json";

        private ListView _downloadItemListView;
        private ListView _downloadCountryListView;
        private Button _backButton;
        
        private List<PoisToDownload> _allItems;

        private List<PoisToDownload> _items;
        private List<PoiCountry> _countries;
        private DownloadItemAdapter _itemsAdapter;

        private PoiDatabase _database;
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

            var json = GpxFileProvider.GetFile(GetUrl(IndexFile));
            /*var json = @"[" +
                       "{\"Id\":\"4d5f2e7b-6a31-4e68-ac92-87009976e602\"," +
                       "\"Description\": \"All mountains in the Czech Republic\"," +
                       "\"Url\": \"http://vrcholky.8u.cz/hory.gpx\"," +
                       "\"Country\":\"CZE\"," +
                       "\"Category\": \"Peaks\" }," +
                       "{\"Id\":\"e366eda1-a958-470e-9a93-6c0b6c16390d\"," +
                       "\"Description\": \"All castles in the Slovakia\"," +
                       "\"Url\": \"http://vrcholky.8u.cz/hrady.gpx\"," +
                       "\"Country\":\"SVN\"," +
                       "\"Category\": \"Castles\" }" +
                       "]";*/

            var itemsToDownload = JsonConvert.DeserializeObject<List<PoisToDownload>>(json);

            SetContentView(Resource.Layout.DownloadActivity);

            _downloadItemListView = FindViewById<ListView>(Resource.Id.DownloadItemListView);
            _downloadCountryListView = FindViewById<ListView>(Resource.Id.DownloadCountryListView);

            var downloadedTask = Database.GetDownloadedPoisAsync();
            downloadedTask.Wait();
            _allItems = downloadedTask.Result.ToList();
            
            foreach (var item in itemsToDownload)
            {
                if (!_allItems.Any(x => x.Id == item.Id))
                {
                    _allItems.Add(item);
                }
            }

            _countries = _allItems.Select(x => x.Country).Distinct().ToList();
            _items = new List<PoisToDownload>();

            _itemsAdapter = new DownloadItemAdapter(this);


            _backButton = FindViewById<Button>(Resource.Id.BackButton);
            _backButton.SetOnClickListener(this);

            var countryAdapter = new DownloadCountryAdapter(this, _countries);
            _downloadCountryListView.Adapter = countryAdapter;
            _downloadCountryListView.ItemClick += OnListCountryClick;

            _itemsAdapter = new DownloadItemAdapter(this);
            _downloadItemListView.Adapter = _itemsAdapter;
            _downloadItemListView.ItemClick += OnListItemClick;

        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
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
            _itemsAdapter.NotifyDataSetChanged();
        }

        void OnListCountryClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            PoiCountry country = _countries[e.Position];
            _items = _allItems.Where(x => x.Country == country).ToList();
            _itemsAdapter.SetItems(_items);
        }

        public void OnClick(View v)
        {
            Finish();
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

                PopupDialog("Information", $"{listOfPoi.Count()} items loaded to database.");
            }
            catch (Exception ex)
            {
                PopupDialog("Error", $"Error when loading data. {ex.Message}");
            }
        }

        private void DeleteFromInternet(PoisToDownload source)
        {
            try
            {
                Database.DeleteAllFromSource(source.Id);
                
                source.DownloadDate = null;
                Database.DeleteItem(source);

                PopupDialog("Information", $"Items removed from database.");
            }
            catch (Exception ex)
            {
                PopupDialog("Error", $"Error when removing data. {ex.Message}");
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

        public string GetUrl(string path)
        {
            return WebsiteUrl + path;
        }
    }
}