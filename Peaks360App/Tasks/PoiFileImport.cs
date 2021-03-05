using System;
using System.Collections.Generic;
using Android.OS;
using Peaks360Lib.Utilities;
using Peaks360App.Providers;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;

namespace Peaks360App.Tasks
{
    public class PoiFileImport : AsyncTask<string, string, PoiList>
    {
        private PoisToDownload _source;

        public Action<PoiList> OnFinishedAction;
        public Action<int, int> OnStageChange;
        public Action<int> OnProgressChange;
        public Action<string> OnError;

        public PoiFileImport(PoisToDownload source)
        {
            _source = source;
        }

        protected override void OnProgressUpdate(params string[] values)
        {
            base.OnProgressUpdate(values);
        }

        protected override void OnPreExecute()
        {
            base.OnPreExecute();
        }

        protected override void OnPostExecute(PoiList result)
        {
            base.OnPostExecute(result);
            OnFinishedAction?.Invoke(result);
        }

        protected override PoiList RunInBackground(params string[] @url)
        {
            try
            {
                OnStageChange?.Invoke(Resource.String.Download_Progress_Downloading, 1);
                var file = GpxFileProvider.GetFile(GpxFileProvider.GetUrl(url[0]));
                OnProgressChange?.Invoke(1);

                var listOfPoi = GpxFileParser.Parse(file, _source.Category, _source.Country, _source.Id,
                    x => OnStageChange?.Invoke(Resource.String.Download_Progress_Processing, x),
                    x => OnProgressChange?.Invoke(x));

                return listOfPoi;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ExceptionHelper.Exception2ErrorMessage(ex));
                return new PoiList();
            }
        }
    }
}
