using System.Threading.Tasks;
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
using Plugin.InAppBilling;

/*
In App Billing Plugin for Xamarin & Windows


Version 4.0 has significant updates.

1.) You must compile and target against Android 10 or higher
2.) On Android you must handle pending transactions and call AcknowledgePurchaseAsync` when done
3.) On Android HandleActivityResult has been removed.
4.) We now use Xamarin.Essentials and setup is required per docs.

Find the latest setup guides, documentation, and testing instructions at: 
https://github.com/jamesmontemagno/InAppBillingPlugin
*/

namespace Peaks360App.Activities
{
    [Activity(Label = "PaymentActivity")]
    public class PaymentActivity : Activity, View.IOnClickListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            SetContentView(Resource.Layout.PaymentActivity);

            var buttonPay = FindViewById<Android.Widget.Button>(Resource.Id.buttonPay);
            buttonPay.SetOnClickListener(this);
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonPay:
                    Task.Run(async () => { await MakePurchase(); });
                    
                    break;
            }
        }

        public async Task<bool> MakePurchase()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;

                //make additional billing calls
                return true;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

    }
}
