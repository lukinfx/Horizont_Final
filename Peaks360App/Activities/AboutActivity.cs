using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Services;
using Xamarin.Forms;

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
            ActionBar.SetTitle(Resource.String.AboutActivity);

            var versionTextView = FindViewById<TextView>(Resource.Id.aboutVersion);

            var versionNumber = DependencyService.Get<IAppVersionService>().GetVersionNumber();
            var buildNumber = DependencyService.Get<IAppVersionService>().GetBuildNumber();
            var firstInstall = DependencyService.Get<IAppVersionService>().GetInstallDate();
            versionTextView.Text = $"Version: {versionNumber} ({buildNumber}) installed on {firstInstall.ToShortDateString()}";
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