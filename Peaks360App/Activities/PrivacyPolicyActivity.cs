using Android.App;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;

namespace Peaks360App.Activities
{
    [Activity(Label = "PrivacyPolicy")]
    public class PrivacyPolicyActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            SetContentView(Resource.Layout.PrivacyPolicyActivityPortrait);

            var toolbar = FindViewById<Toolbar>(Resource.Id.privacyPolicyToolbar);
            SetActionBar(toolbar);

            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);
            ActionBar.Title = "Privacy Policy";

            var textViewPrivacyPolicy = FindViewById<TextView>(Resource.Id.textViewPrivacyPolicy);
            ISpanned sp = Html.FromHtml(GetString(Resource.String.PrivacyPolicy));
            textViewPrivacyPolicy.SetText(sp, TextView.BufferType.Spannable );
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