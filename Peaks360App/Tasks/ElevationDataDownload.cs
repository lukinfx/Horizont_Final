using System;
using Android.OS;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;

namespace Peaks360App.Tasks
{
    public class ElevationDataDownload : AsyncTask<GpsLocation, string, string>
    {
        private ElevationTileCollection _elevationTileCollection;

        public GpsLocation MyLocation { get; private set; }
        public int MaxDistance { get; private set; }

        public Action<string> OnFinishedAction;
        public Action<int> OnProgressChange;

        public ElevationDataDownload(GpsLocation myLocation, int maxDistance)
        {
            MyLocation = myLocation;
            MaxDistance = maxDistance;
            _elevationTileCollection = new ElevationTileCollection(MyLocation, (int)MaxDistance);
        }

        public int GetCountToDownload()
        {
            return _elevationTileCollection.GetCountToDownload();
        }

        public float GetSizeToDownload()
        {
            return _elevationTileCollection.GetSizeToDownload();
        }

        public long GetSize()
        {
            return _elevationTileCollection.GetSize();
        }

        protected override void OnProgressUpdate(params string[] values)
        {
            base.OnProgressUpdate(values);
            System.Console.Write(".");
        }

        protected override void OnPreExecute()
        {
            base.OnPreExecute();
        }

        protected override void OnPostExecute(string result)
        {
            base.OnPostExecute(result);
            OnFinishedAction?.Invoke(result);
        }

        protected override string RunInBackground(params GpsLocation[] @params)
        {
            try
            {
                var downloadingOk = _elevationTileCollection.Download(progress =>
                {
                    OnProgressChange?.Invoke((int)(progress / (float)_elevationTileCollection.GetCountToDownload() * 100f));
                });

                return downloadingOk ? null : _elevationTileCollection.GetErrorList();
            }
            catch (Exception ex)
            {
                return ExceptionHelper.Exception2ErrorMessage(ex);
            }
        }
    }
}
