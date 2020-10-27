using System;
using System.Collections.Generic;
using Android.OS;
using HorizonLib.Utilities;
using HorizontApp.Providers;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Tasks
{
    public class PoiFileImport : AsyncTask<string, string, PoiList>
    {
        private PoisToDownload _source;

        public Action<PoiList> OnFinishedAction;
        public Action<string, int> OnStageChange;
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
            System.Console.WriteLine("Staring");
        }

        protected override void OnPostExecute(PoiList result)
        {
            base.OnPostExecute(result);
            System.Console.WriteLine("Finished");
            OnFinishedAction(result);
        }

        protected override PoiList RunInBackground(params string[] @url)
        {
            try
            {
                OnStageChange("Downloading data", 1);
                var file = GpxFileProvider.GetFile(GpxFileProvider.GetUrl(url[0]));
                OnProgressChange(1);

                var listOfPoi = GpxFileParser.Parse(file, _source.Category, _source.Id,
                    x => OnStageChange("Parsing data", x),
                    x => OnProgressChange(x));

                return listOfPoi;
            }
            catch (Exception ex)
            {
                OnError(ExceptionHelper.Exception2ErrorMessage(ex));
                return new PoiList();
            }
        }
    }
}
