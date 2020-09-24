using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Models;
using HorizontApp.Utilities;
using HorizontApp.Views;
using Object = Java.Lang.Object;

namespace HorizontApp.Tasks
{
    public class ElevationCalculation : AsyncTask<GpsLocation, string, ElevationProfileData>
    {
        private GpsLocation _myLocation;
        private int _visibility;
        private Action<ElevationProfileData> _onFinishedAction;
        private Action<string, int> _onStageChange;
        private Action<int> _onProgressChange;
        public ElevationCalculation(
            GpsLocation myLocation, 
            int visibility, 
            Action<ElevationProfileData> onFinishedAction,
            Action<string, int> onStageChange,
            Action<int> onProgressChange)
        {
            _myLocation = myLocation;
            _visibility = visibility;
            _onFinishedAction = onFinishedAction;
            _onStageChange = onStageChange;
            _onProgressChange = onProgressChange;
        }

        protected override void OnProgressUpdate(params string[] values)
        {
            base.OnProgressUpdate(values);
            System.Console.Write(".");
        }

        protected override void OnPreExecute()
        {
            base.OnPreExecute();
            System.Console.WriteLine("Staring");
        }

        protected override void OnPostExecute(ElevationProfileData result)
        {
            base.OnPostExecute(result);
            System.Console.WriteLine("Finished");
            _onFinishedAction(result);
        }

        protected override ElevationProfileData RunInBackground(params GpsLocation[] @params)
        {
            _onStageChange("Loading elevation data.", 100);
            _onProgressChange(0);
            var downloadPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            var elevationDataFile = downloadPath.Path + "/ALPSMLC30_N049E018_DSM.tif";
            Thread.Sleep(100);
            _onProgressChange(50);
            GpsUtils.BoundingRect(_myLocation, _visibility, out var min, out var max);
            var elevationData = GeoTiffReader.ReadTiff(elevationDataFile, min, max, _visibility<20 ? 1 : 2);
            Thread.Sleep(100);
            _onProgressChange(100);
            Thread.Sleep(50);

            _onStageChange("Processing elevation data.", elevationData.Count);
            ElevationProfile ep = new ElevationProfile();
            ep.GenerateElevationProfile(_myLocation, _visibility, elevationData, progress =>
            {
                _onProgressChange(progress);
            });
            return ep.GetProfile();
        }
    }
}