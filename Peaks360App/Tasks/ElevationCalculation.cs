using System;
using System.Linq;
using System.Collections.Generic;
using Android.OS;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Tasks
{
    public class ElevationCalculation : AsyncTask<GpsLocation, string, ElevationProfileData>
    {
        private ElevationTileCollection _elevationTileCollection;

        public GpsLocation MyLocation { get; private set; }
        public int MaxDistance { get; private set; }

        public Action<ElevationProfileData> OnFinishedAction;
        public Action<int> OnProgressChange;

        public ElevationCalculation(GpsLocation myLocation, int maxDistance)
        {
            MyLocation = myLocation;
            MaxDistance = maxDistance;
            _elevationTileCollection = new ElevationTileCollection(MyLocation, (int)MaxDistance);
        }

        public float GetSizeToDownload()
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

        class Part
        {
            public string Title { get; set; }
            public int TimeComplexity { get; set; }
            public int Count { get; set; }
        }
        protected override ElevationProfileData RunInBackground(params GpsLocation[] @params)
        {
            try
            {
                var parts = new List<Part>
                {
                    new Part() { Title = "DownloadTile", TimeComplexity = 50, Count = _elevationTileCollection.GetCountToDownload() },
                    new Part() { Title = "ReadTile"    , TimeComplexity = 10, Count = _elevationTileCollection.GetCount() },
                    new Part() { Title = "Generate"    , TimeComplexity =  1, Count = 360 },
                    new Part() { Title = "MakeLines"   , TimeComplexity =  1, Count = 360 }
                };

                float totalTimeComplexity = parts.Select(x => x.Count * x.TimeComplexity).Aggregate((sum, part) => sum + part);
                int totalProgress = 0;

                var downloadingOk = _elevationTileCollection.Download(progress =>
                {
                    var localProgress = progress * parts[0].TimeComplexity;
                    OnProgressChange?.Invoke((int)((totalProgress + localProgress) / totalTimeComplexity * 100f));
                });
                totalProgress += parts[0].Count * parts[0].TimeComplexity;

                var readingOk = _elevationTileCollection.Read(progress =>
                {
                    var localProgress = progress * parts[1].TimeComplexity;
                    OnProgressChange?.Invoke((int)((totalProgress + localProgress) / totalTimeComplexity * 100f));
                });
                totalProgress += parts[1].Count * parts[1].TimeComplexity;

                ElevationDataGenerator ep = new ElevationDataGenerator();
                ep.Generate(MyLocation, MaxDistance, _elevationTileCollection, progress =>
                {
                    var localProgress = progress * parts[2].TimeComplexity;
                    OnProgressChange?.Invoke((int)((totalProgress + localProgress) / totalTimeComplexity * 100f));
                });
                totalProgress += parts[2].Count * parts[2].TimeComplexity;


                ElevationProfile ep2 = new ElevationProfile();
                ep2.GenerateElevationProfile3(MyLocation, MaxDistance, ep.GetProfile(), progress =>
                {
                    var localProgress = progress * parts[3].TimeComplexity;
                    OnProgressChange?.Invoke((int)((totalProgress + localProgress) / totalTimeComplexity * 100f));
                });
                totalProgress += parts[3].Count * parts[3].TimeComplexity;

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
