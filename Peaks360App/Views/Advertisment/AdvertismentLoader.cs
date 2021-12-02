using System;
using Android.App;
using Android.Gms.Ads;
using Android.Gms.Ads.Interstitial;

namespace Peaks360App.Views.Advertisment
{
    public class AdvertismentLoader : InterstitialCallback
    {
        private InterstitialAd _interstitialAd;
        private Activity _activity;
        private string _adUnitId;

        public AdvertismentLoader(Activity activity, string adUnitId)
        {
            _activity = activity;
            _adUnitId = adUnitId;
        }

        public override void OnAdLoaded(Android.Gms.Ads.Interstitial.InterstitialAd interstitialAd)
        {
            _interstitialAd = interstitialAd;
            base.OnAdLoaded(interstitialAd);
        }

        public override void OnAdFailedToLoad(LoadAdError p0)
        {
            base.OnAdFailedToLoad(p0);
            _interstitialAd = null;
            Console.WriteLine(p0.Message);
        }

        public void Show()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.FullScreenContentCallback = new CompletitionCallback(this);
                _interstitialAd.Show(_activity);
            }
        }

        internal bool IsLoaded()
        {
            return _interstitialAd != null;
        }

        internal void RequestNew()
        {
            AdRequest adRequest = new AdRequest.Builder().Build();
            InterstitialAd.Load(_activity, _adUnitId, adRequest, this);
        }

        internal void Clear()
        {
            _interstitialAd = null;
        }
    }
}