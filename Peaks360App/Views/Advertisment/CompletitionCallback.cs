using Android.Gms.Ads;

namespace Peaks360App.Views.Advertisment
{
    public class CompletitionCallback : FullScreenContentCallback
    {
        private AdvertismentLoader _loader;

        public CompletitionCallback(AdvertismentLoader loader)
        {
            _loader = loader;
        }

        public override void OnAdDismissedFullScreenContent()
        {
            // Called when fullscreen content is dismissed.
            base.OnAdDismissedFullScreenContent();
        }

        public override void OnAdFailedToShowFullScreenContent(AdError p0)
        {
            // Called when fullscreen content failed to show.
            base.OnAdFailedToShowFullScreenContent(p0);
            _loader.Clear();
        }

        public override void OnAdShowedFullScreenContent()
        {
            // Called when fullscreen content is shown.
            base.OnAdShowedFullScreenContent();
            _loader.Clear();
        }
    }
}