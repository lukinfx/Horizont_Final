using System;
using System.Collections.Generic;
using Android.OS;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using Peaks360App.Providers;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using Newtonsoft.Json;

namespace Peaks360App.Tasks
{
    public class ElevationDataImport : AsyncTask<string, string, bool >
    {
        public static string COMMAND_DOWNLOAD = "Download";
        public static string COMMAND_REMOVE = "Remove";
        private PoisToDownload _source;
        private ElevationTileCollection _elevationTileCollection;

        public Action<bool> OnFinishedAction;
        public Action<int, int> OnStageChange;
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
        }

        protected override void OnPostExecute(bool result)
        {
            base.OnPostExecute(result);
            OnFinishedAction?.Invoke(result);
        }

        protected override bool RunInBackground(params string[] @command)
        {
            try
            {
                OnStageChange?.Invoke(Resource.String.Download_Progress_FetchingElevationTilesList, 1);
                var file = GpxFileProvider.GetFile(GpxFileProvider.GetUrl(_source.Url));
                OnProgressChange?.Invoke(1);
                var elevationMap = JsonConvert.DeserializeObject<ElevationMap>(file);

                _elevationTileCollection = new ElevationTileCollection(elevationMap);

                if (@command[0] == COMMAND_DOWNLOAD)
                {
                    OnStageChange?.Invoke(Resource.String.Download_Progress_DownloadingElevationData, _elevationTileCollection.GetCountToDownload());
                    if (!_elevationTileCollection.Download(progress => { OnProgressChange(progress); }))
                    {
                        OnError?.Invoke(_elevationTileCollection.GetErrorList());
                        return false;
                    }

                    return true;
                }
                
                if(@command[0] == COMMAND_REMOVE)
                {
                    OnStageChange?.Invoke(Resource.String.Download_Progress_RemovingElevationData, 1);
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
