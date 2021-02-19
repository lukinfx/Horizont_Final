using System;
using System.Collections.Generic;
using Android.OS;
using Java.Lang;
using Peaks360Lib.Utilities;
using Peaks360App.Providers;
using Peaks360Lib.Domain.Models;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using Exception = System.Exception;

namespace Peaks360App.Tasks
{
    public class FileDownload : AsyncTask<string, string, string>
    {
        public Action<string> OnFinishedAction;
        public Action<string> OnError;

        public FileDownload()
        {
        }

        protected override void OnProgressUpdate(params string[] values)
        {
            base.OnProgressUpdate(values);
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

        protected override string RunInBackground(params string[] @url)
        {
            try
            {
                var file = GpxFileProvider.GetFile(url[0]);
                Thread.Sleep(500);
                return file;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ExceptionHelper.Exception2ErrorMessage(ex));
                return null; 
            }
        }
    }
}
