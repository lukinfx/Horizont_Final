using Android.App;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Peaks360App.AppContext;
using Peaks360App.Utilities;
using static Android.Views.View;

namespace Peaks360App.Activities
{
    [Activity(Label = "PrivacyPolicy")]
    public class PrivacyPolicyActivity : Activity, IOnClickListener
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
            ActionBar.SetTitle(Resource.String.PrivacyPolicyStatement);

            ISpanned sp = Html.FromHtml(GetString(Resource.String.PrivacyPolicy));
            FindViewById<TextView>(Resource.Id.textViewPrivacyPolicy).SetText(sp, TextView.BufferType.Spannable );

            FindViewById<Button>(Resource.Id.buttonPrivacyPolicyAgreement).SetOnClickListener(this);
            FindViewById<Button>(Resource.Id.buttonPrivacyPolicyRejection).SetOnClickListener(this);

            FindViewById<Button>(Resource.Id.buttonPrivacyPolicyAgreement).Visibility =
                AppContextLiveData.Instance.Settings.IsPrivacyPolicyApprovementNeeded() ? ViewStates.Visible : ViewStates.Gone;
            FindViewById<Button>(Resource.Id.buttonPrivacyPolicyRejection).Visibility =
                AppContextLiveData.Instance.Settings.IsPrivacyPolicyApprovementNeeded() ? ViewStates.Visible : ViewStates.Gone;
        }

        public override void OnBackPressed()
        {
            if (AppContextLiveData.Instance.Settings.IsPrivacyPolicyApprovementNeeded())
            {

                PopupHelper.Toast(this, "We are sorry, but you have to scroll down and express your consent with privacy policy.");
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonPrivacyPolicyAgreement:
                    AppContextLiveData.Instance.Settings.PrivacyPolicyApproved();
                    Finish();
                    break;
                case Resource.Id.buttonPrivacyPolicyRejection:
                    System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                    break;
            }
        }
    }
}