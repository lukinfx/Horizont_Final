using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Peaks360App.AppContext;
using Peaks360App.Services;
using View = Android.Views.View;

namespace Peaks360App.Activities
{
    [Activity(Label = "@string/AboutActivity")]
    public class AboutActivity : Activity, View.IOnClickListener
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

            var buttonPrivacyPolicy = FindViewById<TextView>(Resource.Id.textViewPrivacyPolicyLink);
            ISpanned sp = Html.FromHtml($"<a href=''>{GetString(Resource.String.PrivacyPolicyStatement)}</a>");
            buttonPrivacyPolicy.SetText(sp, TextView.BufferType.Spannable);
            buttonPrivacyPolicy.SetOnClickListener(this);
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.textViewPrivacyPolicyLink:
                    Intent privacyPolicyActivityIntent = new Intent(this, typeof(PrivacyPolicyActivity));
                    StartActivity(privacyPolicyActivityIntent);
                    break;
            }
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