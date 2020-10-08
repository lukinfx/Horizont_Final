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
using HorizonLib.Utilities;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontApp.Providers;
using HorizontApp.Views;
using HorizontLib.Utilities;
using HorizontLib.Providers;
using GpsUtils = HorizontApp.Utilities.GpsUtils;
using Object = Java.Lang.Object;

namespace HorizontApp.Tasks
{
    public class ElevationCalculation : AsyncTask<GpsLocation, string, ElevationProfileData>
    {
        private GpsLocation _myLocation;
        private int _visibility;
        private GpsLocation _boundingRectMin, _boundingRectMax;

        private List<string> noDataTiles = new List<string>();
        private ElevationTileCollection _elevationTileCollection;

        public Action<ElevationProfileData> OnFinishedAction;
        public Action<string, int> OnStageChange;
        public Action<int> OnProgressChange;

        public ElevationCalculation(GpsLocation myLocation, int visibility)
        {
            _myLocation = myLocation;
            _visibility = visibility;
            GpsUtils.BoundingRect(_myLocation, _visibility, out _boundingRectMin, out _boundingRectMax);

            _elevationTileCollection = new ElevationTileCollection(_myLocation, (int)_visibility);
        }

        public int GetSizeToDownload()
        {
            return _elevationTileCollection.GetSizeToDownload();
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
            OnFinishedAction(result);
        }

        protected override ElevationProfileData RunInBackground(params GpsLocation[] @params)
        {
            try
            {
                OnStageChange("Downloading elevation data", _elevationTileCollection.GetCountToDownload());
                _elevationTileCollection.Download(progress => { OnProgressChange(progress); });
                
                OnStageChange("Reading elevation data", _elevationTileCollection.GetCount());
                _elevationTileCollection.Read(progress => { OnProgressChange(progress); });

                OnStageChange("Preparing elevation data.", 360);
                ElevationDataGenerator ep = new ElevationDataGenerator();
                ep.Generate(_myLocation, _elevationTileCollection, progress => { OnProgressChange(progress); });

                OnStageChange("Processing elevation data.", 360);
                ElevationProfile ep2 = new ElevationProfile();
                ep2.GenerateElevationProfile3(_myLocation, _visibility, ep.GetProfile(), progress => { OnProgressChange(progress); });
                var epd  = ep2.GetProfile();

                epd.ErrorMessage = GetErrorList();

                return epd;
            }
            catch (Exception ex)
            {
                return new ElevationProfileData(ExceptionHelper.Exception2ErrorMessage(ex));
            }

        }

        private string GetErrorList()
        {
            if (!noDataTiles.Any())
                return "";

            string errorsAsString = noDataTiles.Aggregate((agg, item) =>
            {
                return agg + (string.IsNullOrEmpty(agg) ? "" : ",") + item;
            });

            return $"Some tiles were not loaded. The elevation profile may not be complete. \r\n\r\nMissing tiles: {errorsAsString}";
        }

        private List<GpsLocation> ReadElevationData(GpsLocation min, GpsLocation max)
        {
            int tileCount =
                ((int)max.Latitude - (int)min.Latitude + 1)
                * ((int)max.Longitude - (int)min.Longitude + 1);

            OnStageChange("Loading elevation data.", tileCount);
            var elevationData = new List<GpsLocation>();
            int tileProgress = 0;
            for (var lat = (int)min.Latitude; lat < ((int)max.Latitude) + 1; lat++)
            {
                for (var lon = (int)min.Longitude; lon < ((int)max.Longitude) + 1; lon++)
                {
                    ReadElevationFile(lat, lon, min, max, elevationData);
                    tileProgress++;
                    OnProgressChange(tileProgress);
                }
            }

            return elevationData;
        }

        private void ReadElevationFile(int lat, int lon, GpsLocation min, GpsLocation max, List<GpsLocation> elevationData)
        {
            try
            {
                var filePath = ElevationFileProvider.GetElevationFile(lat, lon);
                GeoTiffReader.ReadTiff(filePath, min, max, _myLocation, _visibility < 20 ? 1 : 2, elevationData);
            }
            catch (Exception ex)
            {
                noDataTiles.Add($"{ex.Message}(Lat/Lon:{lat}/{lon})");
            }
        }
    }
}
