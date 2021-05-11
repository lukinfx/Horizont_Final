using System;
using Android.Content.PM;
using Xamarin.Forms;
using Peaks360App.Services;

[assembly: Dependency(typeof(AppVersionService))]
namespace Peaks360App.Services
{
    public interface IAppVersionService
    {
        string GetVersionNumber();
        string GetBuildNumber();
        DateTime GetInstallDate();
    }

    public class AppVersionService : IAppVersionService
    {
        PackageInfo _appInfo;
        public AppVersionService()
        {
            var context = Android.App.Application.Context;
            _appInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
        }

        public string GetVersionNumber()
        {
            return _appInfo.VersionName;
        }
        public string GetBuildNumber()
        {
            return _appInfo.VersionCode.ToString();
        }

        public DateTime GetInstallDate()
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(_appInfo.FirstInstallTime);
        }
    }

}

