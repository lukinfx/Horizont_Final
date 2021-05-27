using System;
using System.Threading;
using Android.App;
using Android.Content;
using Xamarin.Essentials;
using Peaks360App.AppContext;
using Peaks360App.Tasks;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Providers
{
    public class ElevationProfileChangedEventArgs : EventArgs { public ElevationProfileData ElevationProfileData; }
    public delegate void ElevationProfileChangedEventHandler(object sender, ElevationProfileChangedEventArgs e);

    public class ElevationProfileProvider
    {
        private bool _elevationProfileBeingGenerated = false;

        public event ElevationProfileChangedEventHandler ElevationProfileChanged;

        //private Resources Resources { get { return Application.Context.Resources; } }

        private static ElevationProfileProvider _instance;
        public static ElevationProfileProvider Instance()
        {
            if (_instance == null)
            {
                _instance = new ElevationProfileProvider();
            }
            return _instance;
        }

        private ElevationProfileProvider()
        {

        }

        public void CheckAndReloadElevationProfile(Context appContext, int maxDistance, IAppContext context)
        {
            if (context.Settings.ShowElevationProfile)
            {
                if (GpsUtils.HasAltitude(context.MyLocation))
                {
                    if (_elevationProfileBeingGenerated == false)
                    {
                        if (context.ElevationProfileData == null || !context.ElevationProfileData.IsValid(context.MyLocation, context.Settings.MaxDistance))
                        {
                            GenerateElevationProfile(appContext, maxDistance, context.MyLocation);
                        }
                        else
                        {
                            RefreshElevationProfile(context.ElevationProfileData);
                        }
                    }
                }
            }
        }

        private void GenerateElevationProfile(Context appContext, int maxDistance, GpsLocation myLocation)
        {
            try
            {
                if (!GpsUtils.HasAltitude(myLocation))
                {
                    PopupHelper.ErrorDialog(appContext, Resource.String.Main_ErrorUnknownAltitude);
                    return;
                }

                _elevationProfileBeingGenerated = true;

                var ec = new ElevationCalculation(myLocation, maxDistance);

                var size = ec.GetSizeToDownload();
                if (size == 0)
                {
                    StartDownloadAndCalculate(appContext, ec);
                    return;
                }

                using (var builder = new AlertDialog.Builder(appContext))
                {
                    builder.SetCancelable(false);
                    builder.SetTitle(appContext.Resources.GetText(Resource.String.Common_Question));
                    builder.SetMessage(String.Format(appContext.Resources.GetText(Resource.String.Download_Confirmation), size));
                    builder.SetIcon(Android.Resource.Drawable.IcMenuHelp);
                    builder.SetPositiveButton(appContext.Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) => { StartDownloadAndCalculateAsync(appContext, ec); });
                    builder.SetNegativeButton(appContext.Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { _elevationProfileBeingGenerated = false; });

                    var myCustomDialog = builder.Create();

                    myCustomDialog.Show();
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(appContext, Resource.String.Main_ErrorGeneratingElevationProfile, ex.Message);
            }
        }

        private void StartDownloadAndCalculate(Context appContext, ElevationCalculation ec)
        {
            _elevationProfileBeingGenerated = true;
            var lastProgressUpdate = System.Environment.TickCount;

            var pd = new ProgressDialog(appContext);
            pd.SetMessage(appContext.Resources.GetText(Resource.String.Main_GeneratingElevationProfile));
            pd.SetCancelable(false);
            pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
            pd.Max = 100;
            pd.Show();

            ec.OnFinishedAction = (result) =>
            {
                pd.Hide();
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    PopupHelper.ErrorDialog(appContext, result.ErrorMessage);
                }

                RefreshElevationProfile(result);
                _elevationProfileBeingGenerated = false;
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

            ec.Execute();
        }

        private void StartDownloadAndCalculateAsync(Context appContext, ElevationCalculation ec)
        {
            try
            {
                StartDownloadAndCalculate(appContext, ec);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(appContext, Resource.String.Main_ErrorGeneratingElevationProfile, ex.Message);
            }
        }

        private void RefreshElevationProfile(ElevationProfileData elevationProfileData)
        {
            if (elevationProfileData != null)
            {
                ElevationProfileChanged?.Invoke(this, new ElevationProfileChangedEventArgs() {ElevationProfileData = elevationProfileData});
            }
        }
    }
}