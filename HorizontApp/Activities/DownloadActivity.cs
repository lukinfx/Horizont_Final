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

        private ListView downloadItemListView;
        private Button backButton;
        private List<PoisToDownload> items;
        private PoiDatabase database;
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
            items = JsonConvert.DeserializeObject<List<PoisToDownload>>(json);

            SetContentView(Resource.Layout.DownloadActivity);

            downloadItemListView = FindViewById<ListView>(Resource.Id.DownloadItemListView);
            backButton = FindViewById<Button>(Resource.Id.BackButton);
            backButton.SetOnClickListener(this);

            var adapter = new DownloadItemAdapter(this, items);
            downloadItemListView.Adapter = adapter;
            downloadItemListView.ItemClick += OnListItemClick;
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            PoisToDownload item = items[e.Position];
            LoadDataFromInternet(GetUrl(item.Url), item.Category);
        }

        public void OnClick(View v)
        {
            Finish();
        }

        private async void LoadDataFromInternet(string filePath, PoiCategory category)
        {
            try
            {
                var file = GpxFileProvider.GetFile(filePath);
                var listOfPoi = GpxFileParser.Parse(file, category);
                await Database.InsertAllAsync(listOfPoi);

                PopupDialog("Information", $"{listOfPoi.Count()} items loaded to database.");
            }
            catch (Exception ex)
            {
                PopupDialog("Error", $"Error when loading data. {ex.Message}");
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