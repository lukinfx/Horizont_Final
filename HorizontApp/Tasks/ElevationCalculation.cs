using System;
using Android.OS;
using HorizonLib.Utilities;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Tasks
{
    public class ElevationCalculation : AsyncTask<GpsLocation, string, ElevationProfileData>
    {
        private GpsLocation _myLocation;
        private int _visibility;
        private ElevationTileCollection _elevationTileCollection;

        public Action<ElevationProfileData> OnFinishedAction;
        public Action<string, int> OnStageChange;
        public Action<int> OnProgressChange;

        public ElevationCalculation(GpsLocation myLocation, int visibility)
        {
            _myLocation = myLocation;
            _visibility = visibility;
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
                bool downloadingOk = _elevationTileCollection.Download(progress => { OnProgressChange(progress); });
                
                OnStageChange("Reading elevation data", _elevationTileCollection.GetCount());
                bool readingOk = _elevationTileCollection.Read(progress => { OnProgressChange(progress); });

                OnStageChange("Preparing elevation data.", 360);
                ElevationDataGenerator ep = new ElevationDataGenerator();
                ep.Generate(_myLocation, _elevationTileCollection, progress => { OnProgressChange(progress); });

                OnStageChange("Processing elevation data.", 360);
                ElevationProfile ep2 = new ElevationProfile();
                ep2.GenerateElevationProfile3(_myLocation, _visibility, ep.GetProfile(), progress => { OnProgressChange(progress); });
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
