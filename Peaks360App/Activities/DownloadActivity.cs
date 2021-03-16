using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Newtonsoft.Json;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Peaks360App.DataAccess;
using Peaks360App.Providers;
using Peaks360App.Tasks;
using Peaks360App.Utilities;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/DownloadActivity")]
    public class DownloadActivity : Activity
    {
        private ListView _downloadItemListView;
        private ListView _downloadCountryListView;
        private Spinner _downloadCountrySpinner;
        private DownloadCountryAdapter _countryAdapter;
        private DownloadItemAdapter _downloadItemAdapter;

        private List<PoisToDownload> _downloadItems;

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
            AppContextLiveData.Instance.SetLocale(this);

            InitializeUI();
        }

        protected override void OnStart()
        {
            base.OnStart();

            DownloadIndex(result => OnIndexDownloaded(result));
        }

        private void InitializeUI()
        {
            _countryAdapter = new DownloadCountryAdapter(this);
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.DownloadActivityPortrait);

                _downloadCountrySpinner = FindViewById<Spinner>(Resource.Id.DownloadCountrySpinner);
                _downloadCountrySpinner.Adapter = _countryAdapter;
                _downloadCountrySpinner.ItemSelected += OnCountrySpinnerItemSelected;

            }
            else
            {
                SetContentView(Resource.Layout.DownloadActivityLandscape);

                _downloadCountryListView = FindViewById<ListView>(Resource.Id.DownloadCountryListView);
                _downloadCountryListView.Adapter = _countryAdapter;
                _downloadCountryListView.ItemClick += OnCountryListItemClicked;
            }

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetTitle(Resource.String.DownloadActivity);

            _downloadItemListView = FindViewById<ListView>(Resource.Id.DownloadItemListView);

            _downloadItemAdapter = new DownloadItemAdapter(this);
            _downloadItemListView.Adapter = _downloadItemAdapter;
            _downloadItemListView.ItemClick += OnDownloadListItemClicked;
        }

        protected override void OnResume()
        {
            base.OnResume();

            SelectDefaultCountry();
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
            PoisToDownload item = _downloadItemAdapter[e.Position];

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
            PoiCountry country = _countryAdapter[position];
            var items = _downloadItems.Where(x => x.Country == country).OrderBy(x => x.Category).ToList();
            _downloadItemAdapter.SetItems(items);
        }

        private void OnIndexDownloaded(string json)
        {
            try
            {
                //fetch list of already downladed items from database
                var downloadedTask = Database.GetDownloadedPoisAsync();
                downloadedTask.Wait();
                _downloadItems = downloadedTask.Result.ToList();

                if (!String.IsNullOrEmpty(json))
                {
                    var _horizonIndex = JsonConvert.DeserializeObject<HorizonIndex>(json);

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
                }
            }
            catch (Exception e)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PopupHelper.ErrorDialog(this,
                        Resources.GetText(Resource.String.Download_ErrorDownloading) + " " + e.Message);
                });

                return;
            }

            var countries = _downloadItems.Select(x => x.Country).Distinct().OrderBy(x => x).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _countryAdapter.SetItems(countries);

                SelectDefaultCountry();
            });
        }

        private void SelectDefaultCountry()
        {
            var defaultCountry = PoiCountryHelper.GetDefaultCountry();
            if (defaultCountry != null)
            {
                var pos = _countryAdapter.GetPosition(defaultCountry.Value);
                if (pos >= 0)
                {
                    _downloadCountrySpinner?.SetSelection(pos);
                    _downloadCountryListView?.SetSelection(pos);
                    OnCountrySelected(pos);
                }
            }
        }

        private void DownloadIndex(Action<string> onFinished)
        {
            try
            {
                var ec = new FileDownload();

                var pd = new ProgressDialog(this);
                pd.SetMessage(Resources.GetText(Resource.String.Download_LoadingData));
                pd.SetCancelable(false);
                pd.Show();

                ec.OnFinishedAction = (result) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        onFinished.Invoke(result);
                        pd.Hide();
                    }); 
                };
                ec.OnError = (message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.Hide();
                        PopupHelper.ErrorDialog(this,
                            Resources.GetText(Resource.String.Download_ErrorDownloading) + " " + message);
                    });
                };

                ec.Execute(GpxFileProvider.GetIndexUrl());
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloading) + " " + ex.Message);
            }
        }

        private void DownloadPoiDataFromInternet(PoisToDownload source)
        {
            try
            {
                var ec = new PoiFileImport(source);

                var lastProgressUpdate = System.Environment.TickCount;

                var pd = new ProgressDialog(this);
                pd.SetMessage(Resources.GetText(Resource.String.Download_LoadingData));
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
                        new ShowToastRunnable(this,
                            String.Format(Resources.GetText(Resource.String.Download_InfoLoadedItems), result.Count))
                            .Run();

                        _downloadItemAdapter.NotifyDataSetChanged();
                    }
                };
                ec.OnStageChange = (resourceStringId, max) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.SetMessage(Resources.GetText(resourceStringId));
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
                        PopupHelper.ErrorDialog(this,
                            Resources.GetText(Resource.String.Download_ErrorDownloading) + " " + message);
                    });
                };

                ec.Execute(source.Url);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloading) + " " + ex.Message);
            }
        }

        private void DeletePoiDataFromInternet(PoisToDownload source)
        {
            try
            {
                Database.DeleteAllFromSource(source.Id);
                
                source.DownloadDate = null;
                Database.DeleteItem(source);

                new ShowToastRunnable(this, Resources.GetText(Resource.String.Download_InfoRemovedItems))
                    .Run();
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorRemoving) + " " + ex.Message);
            }
        }

        private void DownloadElevationDataFromInternet(PoisToDownload source)
        {
            try
            {
                var ec = new ElevationDataImport(source);

                var lastProgressUpdate = System.Environment.TickCount;

                var pd = new ProgressDialog(this);
                pd.SetMessage(Resources.GetText(Resource.String.Download_LoadingData));
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

                        
                        PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.Information),
                            Resources.GetText(Resource.String.Download_InfoLoadedElevation));
                        
                        _downloadItemAdapter.NotifyDataSetChanged();
                    }
                };
                ec.OnStageChange = (resourceStringId, max) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.SetMessage(Resources.GetText(resourceStringId));
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
                        PopupHelper.ErrorDialog(this,
                            Resources.GetText(Resource.String.Download_ErrorDownloadingElevation) + " " + message);
                    });
                };

                ec.Execute(ElevationDataImport.COMMAND_DOWNLOAD);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloadingElevation) + " " + ex.Message);
            }
        }

        private void DeleteElevationDataFromInternet(PoisToDownload source)
        {
            try
            {
                var ec = new ElevationDataImport(source);

                var pd = new ProgressDialog(this);
                pd.SetMessage(Resources.GetText(Resource.String.Download_RemovingData));
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

                        PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.Information),
                            Resources.GetText(Resource.String.Download_InfoRemovedElevation)); 
                    }
                };
                ec.OnStageChange = (resourceStringId, max) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        pd.SetMessage(Resources.GetText(resourceStringId));
                        pd.Max = max;
                    });
                };
                ec.OnError = (message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PopupHelper.ErrorDialog(this,
                            Resources.GetText(Resource.String.Download_ErrorDownloadingElevation) + " " + message);
                    });
                };

                ec.Execute(ElevationDataImport.COMMAND_REMOVE);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloadingElevation) + " " + ex.Message);
            }
        }
    }
}