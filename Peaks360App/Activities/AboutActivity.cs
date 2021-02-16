using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/AboutActivity")]
    public class AboutActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            SetContentView(Resource.Layout.AboutActivity);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);
            // Create your application here

            var versionTextView = FindViewById<TextView>(Resource.Id.aboutVersion);
            var packageInfo = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0);
            var versionName = packageInfo.VersionName;
            var versionCode = packageInfo.VersionCode;
            var firstInstall = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(packageInfo.FirstInstallTime);
            versionTextView.Text = $"Version: {versionName}.{versionCode}, installed on {firstInstall.ToShortDateString()}";
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}