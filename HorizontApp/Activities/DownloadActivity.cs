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
using HorizontApp.Tasks;
using HorizontApp.Utilities;
using Newtonsoft.Json;
using Xamarin.Essentials;
using static Android.Views.View;
using System.Threading;
using HorizonLib.Utilities;

namespace HorizontApp.Activities
{
    [Activity(Label = "DownloadActivity")]
    public class DownloadActivity : Activity
    {
        private ListView _downloadItemListView;
        private ListView _downloadCountryListView;
        private Spinner _downloadCountrySpinner;
        private DownloadItemAdapter _downloadItemAdapter;

        private HorizonIndex _horizonIndex;
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
            var json = GpxFileProvider.GetFile(GpxFileProvider.GetIndexUrl());
            _horizonIndex = JsonConvert.DeserializeObject<HorizonIndex>(json);

            //combine those two lists together
            foreach (var country in _horizonIndex)
            {
                foreach (var item in country.PoiData)
                {
                    if (!_downloadItems.Any(x => x.Id == item.Id))
                    {
                        _downloadItems.Add(new PoisToDownload()
                        {
                            Id = item.Id,
                            Description = item.Description,
                            Category = item.Category,
                            Url = item.Url,
                            Country = country.Country,
                        });
                    }
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
            if (item.Category == PoiCategory.ElevationData)
            {
                if (item.DownloadDate == null)
                {
                    DownloadElevationDataFromInternet(item);
                }
                else
                {
                    DeleteElevationDataFromInternet(item);
                }
            }
            else
            {
                if (item.DownloadDate == null)
                {
                    DownloadPoiDataFromInternet(item);
                }
                else
                {
                    DeletePoiDataFromInternet(item);
                }
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
            _items = _downloadItems.Where(x => x.Country == country).OrderBy(x => x.Category).ToList();
            _downloadItemAdapter.SetItems(_items);
        }

        private void DownloadPoiDataFromInternet(PoisToDownload source)
        {
            try
            {
                var ec = new PoiFileImport(source);

                var lastProgressUpdate = System.Environment.TickCount;

                var pd = new ProgressDialog(this);
                pd.SetMessage("Loading data. Please Wait.");
                pd.SetCancelable(false);
                pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
                pd.Show();

                ec.OnFinishedAction = (result) =>
                {
                    pd.Hide();
                    if (result.Count > 0)
                    {
                        Database.InsertAll(result);
                        source.DownloadDate = DateTime.Now;
                        Database.InsertItem(source);

                        PopupHelper.InfoDialog(this, "Information", $"{result.Count()} items loaded to database.");
                        _downloadItemAdapter.NotifyDataSetChanged();
                    }
                };
                ec.OnStageChange = (text, max) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.SetMessage(text);
                        pd.Max = max;
                    });
                };
                ec.OnProgressChange = (progress) =>
                {
                    if (progress % 100 == 0)
                    {
                        var tickCount = System.Environment.TickCount;
                        if (tickCount - lastProgressUpdate > 100)
                        {
                            MainThread.BeginInvokeOnMainThread(() => { pd.Progress = progress; });
                            Thread.Sleep(50);
                            lastProgressUpdate = tickCount;
                        }
                    }
                };
                ec.OnError = (message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PopupHelper.ErrorDialog(this, "Error", $"Error when downloading POI data. {message}");
                    });
                };

                ec.Execute(source.Url);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when downloading POI data. {ex.Message}");
            }
        }

        private void DeletePoiDataFromInternet(PoisToDownload source)
        {
            try
            {
                Database.DeleteAllFromSource(source.Id);
                
                source.DownloadDate = null;
                Database.DeleteItem(source);

                PopupHelper.InfoDialog(this, "Information", $"POI items removed from database.");
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when removing POI data. {ex.Message}");
            }
        }

        private void DownloadElevationDataFromInternet(PoisToDownload source)
        {
            try
            {
                var ec = new ElevationDataImport(source);

                var lastProgressUpdate = System.Environment.TickCount;

                var pd = new ProgressDialog(this);
                pd.SetMessage("Loading data. Please Wait.");
                pd.SetCancelable(false);
                pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
                pd.Show();

                ec.OnFinishedAction = (result) =>
                {
                    pd.Hide();
                    if (result == true)
                    {
                        source.DownloadDate = DateTime.Now;
                        Database.InsertItem(source);

                        PopupHelper.InfoDialog(this, "Information", $"Elevation data were downloaded.");
                        _downloadItemAdapter.NotifyDataSetChanged();
                    }
                };
                ec.OnStageChange = (text, max) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.SetMessage(text);
                        pd.Max = max;
                    });
                };
                ec.OnProgressChange = (progress) =>
                {
                    var tickCount = System.Environment.TickCount;
                    if (tickCount - lastProgressUpdate > 100)
                    {
                        MainThread.BeginInvokeOnMainThread(() => { pd.Progress = progress; });
                        Thread.Sleep(50);
                        lastProgressUpdate = tickCount;
                    }
                };
                ec.OnError = (message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PopupHelper.ErrorDialog(this, "Error", $"Error when downloading elevation data. {message}");
                    });
                };

                ec.Execute(ElevationDataImport.COMMAND_DOWNLOAD);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when downloading elevation data. {ex.Message}");
            }
        }

        private void DeleteElevationDataFromInternet(PoisToDownload source)
        {
            try
            {
                var ec = new ElevationDataImport(source);

                var pd = new ProgressDialog(this);
                pd.SetMessage("Removing data. Please Wait.");
                pd.SetCancelable(false);
                pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
                pd.Show();

                ec.OnFinishedAction = (result) =>
                {
                    pd.Hide();
                    if (result == true)
                    {
                        source.DownloadDate = null;
                        Database.DeleteItem(source);

                        PopupHelper.InfoDialog(this, "Information", $"Elevation data deleted."); 
                    }
                };
                ec.OnStageChange = (text, max) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.SetMessage(text);
                        pd.Max = max;
                    });
                };
                ec.OnError = (message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PopupHelper.ErrorDialog(this, "Error", $"Error when downloading elevation data. {message}");
                    });
                };

                ec.Execute(ElevationDataImport.COMMAND_REMOVE);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when deleting elevation data. {ex.Message}");
            }
        }
    }
}