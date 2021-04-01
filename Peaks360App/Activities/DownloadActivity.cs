using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
using Android.Content;
using Peaks360App.Models;
using Peaks360Lib.Utilities;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/DownloadActivity")]
    public class DownloadActivity : TabActivity, View.IOnClickListener, IDownloadedElevationDataActionListener
    {
        private ListView _downloadItemListView;
        private ListView _downloadCountryListView;
        private ListView _downloadedElevationDataListView;
        private Spinner _downloadCountrySpinner;
        private DownloadCountryAdapter _countryAdapter;
        private DownloadItemAdapter _downloadItemAdapter;
        private DownloadedElevationDataAdapter _downloadedElevationDataAdapter;

        private List<PoisToDownload> _downloadItems;

        private IAppContext AppContext { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            InitializeUI();

            AppContext.DownloadedElevationDataModel.DownloadedElevationDataAdded += OnDownloadedElevationDataAdded;
            AppContext.DownloadedElevationDataModel.DownloadedElevationDataUpdated += OnDownloadedElevationDataUpdated;
            AppContext.DownloadedElevationDataModel.DownloadedElevationDataDeleted += OnDownloadedElevationDataDeleted;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            AppContext.DownloadedElevationDataModel.DownloadedElevationDataAdded -= OnDownloadedElevationDataAdded;
            AppContext.DownloadedElevationDataModel.DownloadedElevationDataUpdated -= OnDownloadedElevationDataUpdated;
            AppContext.DownloadedElevationDataModel.DownloadedElevationDataDeleted -= OnDownloadedElevationDataDeleted;
        }

        protected override void OnStart()
        {
            base.OnStart();

            if (_downloadItems == null)
            {
                DownloadIndex(result => OnIndexDownloaded(result));
            }

            Task.Run(async () =>
            {
                var downloadedElevationData = await AppContext.Database.GetDownloadedElevationDataAsync();
                _downloadedElevationDataAdapter.SetItems(downloadedElevationData);
            });
        }

        protected override void OnResume()
        {
            base.OnResume();

            SelectDefaultCountry();
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


            var page1 = TabHost.NewTabSpec("tab_test1");
            page1.SetIndicator(Resources.GetText(Resource.String.Common_POIs));
            page1.SetContent(Resource.Id.downloadTabPois);
            TabHost.AddTab(page1);


            var page2 = TabHost.NewTabSpec("tab_test2");
            page2.SetIndicator(Resources.GetText(Resource.String.Common_ElevationData));
            page2.SetContent(Resource.Id.downloadTabEleData);
            TabHost.AddTab(page2);

            TabHost.CurrentTab = 0;

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetTitle(Resource.String.DownloadActivity);

            _downloadItemListView = FindViewById<ListView>(Resource.Id.DownloadItemListView);

            _downloadItemAdapter = new DownloadItemAdapter(this);
            _downloadItemListView.Adapter = _downloadItemAdapter;
            _downloadItemListView.ItemClick += OnDownloadListItemClicked;

            _downloadedElevationDataListView = FindViewById<ListView>(Resource.Id.listViewDownloadedElevationData);
            _downloadedElevationDataAdapter = new DownloadedElevationDataAdapter(this, this);
            _downloadedElevationDataListView.Adapter = _downloadedElevationDataAdapter;

            var _downloadedElevationDataAddButton = FindViewById<Button>(Resource.Id.buttonAddNew);
            _downloadedElevationDataAddButton.SetOnClickListener(this);

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
                var downloadedTask = AppContext.Database.GetDownloadedPoisAsync();
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
                        Resources.GetText(Resource.String.Download_ErrorDownloading), e.Message);
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
                            Resources.GetText(Resource.String.Download_ErrorDownloading), message);
                    });
                };

                ec.Execute(GpxFileProvider.GetIndexUrl());
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloading), ex.Message);
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
                        AppContext.Database.InsertAll(result);
                        source.DownloadDate = DateTime.Now;
                        AppContext.Database.InsertItem(source);
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
                            Resources.GetText(Resource.String.Download_ErrorDownloading), message);
                    });
                };

                ec.Execute(source.Url);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloading), ex.Message);
            }
        }

        private void DeletePoiDataFromInternet(PoisToDownload source)
        {
            try
            {
                AppContext.Database.DeleteAllFromSource(source.Id);
                
                source.DownloadDate = null;
                AppContext.Database.DeleteItem(source);

                new ShowToastRunnable(this, Resources.GetText(Resource.String.Download_InfoRemovedItems))
                    .Run();
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorRemoving), ex.Message);
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
                        AppContext.Database.InsertItem(source);

                        PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.Download_InfoLoadedElevation));
                        
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
                            Resources.GetText(Resource.String.Download_ErrorDownloadingElevation), message);
                    });
                };

                ec.Execute(ElevationDataImport.COMMAND_DOWNLOAD);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloadingElevation), ex.Message);
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
                        AppContext.Database.DeleteItem(source);

                        PopupHelper.InfoDialog(this, Resources.GetText(Resource.String.Download_InfoRemovedElevation)); 
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
                            Resources.GetText(Resource.String.Download_ErrorDownloadingElevation), message);
                    });
                };

                ec.Execute(ElevationDataImport.COMMAND_REMOVE);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this,
                    Resources.GetText(Resource.String.Download_ErrorDownloadingElevation), ex.Message);
            }
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonAddNew:
                    Intent editActivityIntent = new Intent(this, typeof(AddElevationDataActivity));
                    StartActivity(editActivityIntent);
                    break;
            }
        }

        public void OnDedEditRequest(int position)
        {
            var item = _downloadedElevationDataAdapter[position];
            Intent editActivityIntent = new Intent(this, typeof(AddElevationDataActivity));
            editActivityIntent.PutExtra("Id", item.Id);
            StartActivityForResult(editActivityIntent, AddElevationDataActivity.REQUEST_EDIT_DATA);
        }

        public void OnDedDeleteRequest(int position)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
            alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
            {
                var ded = _downloadedElevationDataAdapter[position];
                AppContext.DownloadedElevationDataModel.DeleteItem(ded);
                DeleteElevationTiles(ded);
            });
            alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
            alert.SetMessage(Resources.GetText(Resource.String.DownloadED_DeleteQuestion));
            var answer = alert.Show();
        }

        private void OnDownloadedElevationDataAdded(object sender, DownloadedElevationDataEventArgs e)
        {
            _downloadedElevationDataAdapter.Add(e.data);
        }

        private void OnDownloadedElevationDataUpdated(object sender, DownloadedElevationDataEventArgs e)
        {
            _downloadedElevationDataAdapter.Update(e.data);
        }

        private void OnDownloadedElevationDataDeleted(object sender, DownloadedElevationDataEventArgs e)
        {
            var position = _downloadedElevationDataAdapter.GetPosition(e.data);
            _downloadedElevationDataAdapter.RemoveAt(position);
        }

        private void DeleteElevationTiles(DownloadedElevationData ded)
        {
            var dedTiles = new ElevationTileCollection(new GpsLocation(ded.Longitude, ded.Latitude, 0), ded.Distance);

            Task.Run(async () =>
            {
                var downloadedElevationData = await AppContext.Database.GetDownloadedElevationDataAsync();
                var tilesToBeRemoved = ElevationTileCollection.GetUniqueTilesForRemoval(ded.Id, downloadedElevationData, dedTiles);
                tilesToBeRemoved.Remove();
            });
        }
    }
}