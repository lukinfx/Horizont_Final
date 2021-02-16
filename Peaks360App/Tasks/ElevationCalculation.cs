using System;
using Android.OS;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;

namespace Peaks360App.Tasks
{
    public class ElevationCalculation : AsyncTask<GpsLocation, string, ElevationProfileData>
    {
        private ElevationTileCollection _elevationTileCollection;

        public GpsLocation MyLocation { get; private set; }
        public int MaxDistance { get; private set; }

        public Action<ElevationProfileData> OnFinishedAction;
        public Action<string, int> OnStageChange;
        public Action<int> OnProgressChange;

        public ElevationCalculation(GpsLocation myLocation, int maxDistance)
        {
            MyLocation = myLocation;
            MaxDistance = maxDistance;
            _elevationTileCollection = new ElevationTileCollection(MyLocation, (int)MaxDistance);
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
            OnFinishedAction?.Invoke(result);
        }

        protected override ElevationProfileData RunInBackground(params GpsLocation[] @params)
        {
            try
            {
                OnStageChange("Downloading elevation data", _elevationTileCollection.GetCountToDownload());
                bool downloadingOk = _elevationTileCollection.Download(progress => { OnProgressChange?.Invoke(progress); });
                
                OnStageChange("Reading elevation data", _elevationTileCollection.GetCount());
                bool readingOk = _elevationTileCollection.Read(progress => { OnProgressChange?.Invoke(progress); });

                OnStageChange("Preparing elevation data.", 360);
                ElevationDataGenerator ep = new ElevationDataGenerator();
                ep.Generate(MyLocation, MaxDistance, _elevationTileCollection, progress => { OnProgressChange?.Invoke(progress); });

                OnStageChange("Processing elevation data.", 360);
                ElevationProfile ep2 = new ElevationProfile();
                ep2.GenerateElevationProfile3(MyLocation, MaxDistance, ep.GetProfile(), progress => { OnProgressChange?.Invoke(progress); });
                var epd  = ep2.GetProfile();

                if (!downloadingOk || !readingOk)
                {
                    epd.ErrorMessage = _elevationTileCollection.GetErrorList();
                }

                return epd;
            }
            catch (Exception ex)
            {
                return new ElevationProfileData(ExceptionHelper.Exception2ErrorMessage(ex));
            }

        }
    }
}
