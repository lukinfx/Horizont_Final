using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using Peaks360App.AppContext;
using View = Android.Views.View;
using Plugin.InAppBilling;
using System;
using System.Linq;
using Xamarin.Essentials;

/*
Xamarin InApp billing guidelines
https://jamesmontemagno.github.io/InAppBillingPlugin/

Google play billing system
https://developer.android.com/google/play/billing

GitHub
https://github.com/jamesmontemagno/InAppBillingPlugin

Video Tutorial
https://www.youtube.com/watch?v=KYFM2z5KPq0
 */


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

//Securing In-App Purchases (Receipt Validation)
//https://jamesmontemagno.github.io/InAppBillingPlugin/SecuringPurchases.html

namespace Peaks360App.Activities
{
    [Activity(Label = "PaymentActivity")]
    public class PaymentActivity : Activity, View.IOnClickListener
    {
        private readonly string _productIdPeaks360Pro = "cz.fdevstudio.peaks360.professional";
        private TextView _textViewProductInfo;
        private TextView _textViewCheckResult;
        private TextView _textViewPurchaseResult;
        private TextView _textViewPurchaseAckResult; 

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppContextLiveData.Instance.SetLocale(this);

            SetContentView(Resource.Layout.PaymentActivity);

            _textViewProductInfo = FindViewById<Android.Widget.TextView>(Resource.Id.textViewProductInfo);
            _textViewCheckResult = FindViewById<Android.Widget.TextView>(Resource.Id.textViewCheckResult);
            _textViewPurchaseResult = FindViewById<Android.Widget.TextView>(Resource.Id.textViewPurchaseResult);
            _textViewPurchaseAckResult = FindViewById<Android.Widget.TextView>(Resource.Id.textViewPurchaseAckResult);

            var buttonShow = FindViewById<Android.Widget.Button>(Resource.Id.buttonShow);
            var buttonCheck = FindViewById<Android.Widget.Button>(Resource.Id.buttonCheck);
            var buttonBuy = FindViewById<Android.Widget.Button>(Resource.Id.buttonBuy);

            buttonShow.SetOnClickListener(this);
            buttonCheck.SetOnClickListener(this);
            buttonBuy.SetOnClickListener(this);
        }

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonShow:
                    Task.Run(async () => { await ListProducts(); });
                    break;
                case Resource.Id.buttonCheck:
                    Task.Run(async () => { await CheckPurchase(); });
                    break;
                case Resource.Id.buttonBuy:
                    Task.Run(async () => { await MakePurchase(); });
                    break;
            }
        }

        private void UpdateTextView(TextView textView, string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                textView.Text = text;
            });
        }

        public async Task<bool> CheckPurchase()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    //we are offline or can't connect, don't try to purchase
                    UpdateTextView(_textViewCheckResult, "Not connected");
                    return false;
                }

                //check purchases
                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);

                if (purchases?.Any(p => p.ProductId == _productIdPeaks360Pro) ?? false)
                {
                    //Purchase restored
                    UpdateTextView(_textViewCheckResult, "Purchased");
                    return true;
                }
                else
                {
                    //no purchases found
                    UpdateTextView(_textViewCheckResult, "NOT Purchased");
                    return false;
                }
            }
            catch (InAppBillingPurchaseException purchaseEx)
            {
                var message = string.Empty;
                switch (purchaseEx.PurchaseError)
                {
                    case PurchaseError.AppStoreUnavailable:
                        message = "Currently the app store seems to be unavailble. Try again later.";
                        break;
                    case PurchaseError.BillingUnavailable:
                        message = "Billing seems to be unavailable, please try again later.";
                        break;
                    case PurchaseError.PaymentInvalid:
                        message = "Payment seems to be invalid, please try again.";
                        break;
                    case PurchaseError.PaymentNotAllowed:
                        message = "Payment does not seem to be enabled/allowed, please try again.";
                        break;
                }

                /*//Decide if it is an error we care about
                if (string.IsNullOrWhiteSpace(message))
                    return false;

                //Display message to user*/
                Console.WriteLine("Purchase issue: " + message);
                UpdateTextView(_textViewCheckResult, message);

                return false;
            }
            catch (Exception ex)
            {
                //Something else has gone wrong, log it
                Console.WriteLine("Issue connecting: " + ex.Message);
                UpdateTextView(_textViewCheckResult, "Issue connecting: " + ex.Message);
                return false;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        public async Task<bool> ListProducts()
        {
            string[] productIds = new string[] { _productIdPeaks360Pro };

            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    //we are offline or can't connect, don't try to purchase
                    UpdateTextView(_textViewProductInfo, "Not connected");
                    return false;
                }

                var items = await billing.GetProductInfoAsync(ItemType.InAppPurchase, productIds);
                if (items.Count() != 1)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _textViewProductInfo.Text = $"None or more that one product found!!! ({items.Count()})";
                    });
                    
                    return false;
                }
                //foreach (var item in items)
                var item = items.Single();
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _textViewProductInfo.Text = $"Product name {item.Name},  desc:{item.Description}, price:{item.LocalizedPrice} {item.CurrencyCode}, id:{item.ProductId}";
                    }); 
                }

                return true;
            }
            catch (InAppBillingPurchaseException purchaseEx)
            {
                var message = string.Empty;
                switch (purchaseEx.PurchaseError)
                {
                    case PurchaseError.AppStoreUnavailable:
                        message = "Currently the app store seems to be unavailble. Try again later.";
                        break;
                    case PurchaseError.BillingUnavailable:
                        message = "Billing seems to be unavailable, please try again later.";
                        break;
                    case PurchaseError.PaymentInvalid:
                        message = "Payment seems to be invalid, please try again.";
                        break;
                    case PurchaseError.PaymentNotAllowed:
                        message = "Payment does not seem to be enabled/allowed, please try again.";
                        break;
                }

                /*//Decide if it is an error we care about
                if (string.IsNullOrWhiteSpace(message))
                    return false;

                //Display message to user*/
                Console.WriteLine("Purchase issue: " + message);
                UpdateTextView(_textViewProductInfo, message);
                return false;
            }
            catch (Exception ex)
            {
                //Something else has gone wrong, log it
                Console.WriteLine("Issue connecting: " + ex.Message);
                UpdateTextView(_textViewProductInfo, "Issue connecting: " + ex.Message);
                return false;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        public async Task<bool> MakePurchase()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    //we are offline or can't connect, don't try to purchase
                    UpdateTextView(_textViewPurchaseResult, "Not connected");
                    return false;
                }

                //check purchases
                string productId = _productIdPeaks360Pro;
                var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase);

                //possibility that a null came through.
                if (purchase == null)
                {
                    UpdateTextView(_textViewPurchaseResult, "Purchase failed - no result");
                    return false;
                }
                
                if (purchase.State == PurchaseState.Purchased)
                {
                    UpdateTextView(_textViewPurchaseResult, "Purchase completed");

                    // Must call AcknowledgePurchaseAsync else the purchase will be refunded
                    var billingResult = await billing.AcknowledgePurchaseAsync(purchase.PurchaseToken);

                    if (billingResult == true)
                    {
                        // whatever you need to do with the result
                        UpdateTextView(_textViewPurchaseAckResult, "Purchase acknowledgement - completed");
                        return true;
                    }
                    else
                    {
                        UpdateTextView(_textViewPurchaseResult, "Purchase acknowledgement - failed");
                        return false;
                    }
                }
                else
                {
                    UpdateTextView(_textViewPurchaseResult, $"Purchase failed - state:{purchase.State.ToString()}");
                    return false;
                }

                //make additional billing calls
            }
            catch (InAppBillingPurchaseException purchaseEx)
            {
                var message = string.Empty;
                switch (purchaseEx.PurchaseError)
                {
                    case PurchaseError.AppStoreUnavailable:
                        message = "Currently the app store seems to be unavailble. Try again later.";
                        break;
                    case PurchaseError.BillingUnavailable:
                        message = "Billing seems to be unavailable, please try again later.";
                        break;
                    case PurchaseError.PaymentInvalid:
                        message = "Payment seems to be invalid, please try again.";
                        break;
                    case PurchaseError.PaymentNotAllowed:
                        message = "Payment does not seem to be enabled/allowed, please try again.";
                        break;
                }

                /*//Decide if it is an error we care about
                if (string.IsNullOrWhiteSpace(message))
                    return false;

                //Display message to user*/
                Console.WriteLine("Purchase issue: " + message);
                UpdateTextView(_textViewPurchaseResult, message);

                return false;
            }
            catch (Exception ex)
            {
                //Something else has gone wrong, log it
                Console.WriteLine("Issue connecting: " + ex.Message);
                UpdateTextView(_textViewPurchaseResult, "Issue connecting: " + ex.Message);
                return false;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

    }
}
