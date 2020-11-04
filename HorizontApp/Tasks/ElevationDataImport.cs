using System;
using System.Collections.Generic;
using Android.OS;
using HorizonLib.Domain.Models;
using HorizonLib.Utilities;
using HorizontApp.Providers;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;
using Newtonsoft.Json;

namespace HorizontApp.Tasks
{
    public class ElevationDataImport : AsyncTask<string, string, bool >
    {
        public static string COMMAND_DOWNLOAD = "Download";
        public static string COMMAND_REMOVE = "Remove";
        private PoisToDownload _source;
        private ElevationTileCollection _elevationTileCollection;

        public Action<bool> OnFinishedAction;
        public Action<string, int> OnStageChange;
        public Action<int> OnProgressChange;
        public Action<string> OnError;

        public ElevationDataImport(PoisToDownload source)
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

        protected override void OnPostExecute(bool result)
        {
            base.OnPostExecute(result);
            System.Console.WriteLine("Finished");
            OnFinishedAction?.Invoke(result);
        }

        protected override bool RunInBackground(params string[] @command)
        {
            try
            {
                OnStageChange?.Invoke("Fetching list of elevation tiles", 1);
                var file = GpxFileProvider.GetFile(GpxFileProvider.GetUrl(_source.Url));
                OnProgressChange?.Invoke(1);
                var elevationMap = JsonConvert.DeserializeObject<ElevationMap>(file);

                _elevationTileCollection = new ElevationTileCollection(elevationMap);

                if (@command[0] == COMMAND_DOWNLOAD)
                {
                    OnStageChange?.Invoke("Downloading elevation data", _elevationTileCollection.GetCountToDownload());
                    if (!_elevationTileCollection.Download(progress => { OnProgressChange(progress); }))
                    {
                        OnError?.Invoke(_elevationTileCollection.GetErrorList());
                        return false;
                    }

                    return true;
                }
                
                if(@command[0] == COMMAND_REMOVE)
                {
                    OnStageChange?.Invoke("Removing elevation data", 1);
                    return _elevationTileCollection.Remove();
                }

                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ExceptionHelper.Exception2ErrorMessage(ex));
                return false;
            }
        }
    }
}
