using Android.App;
using Android.Content;
using Xamarin.Essentials;

namespace Peaks360App.Utilities
{
    public class PopupHelper
    {
        public static void ErrorDialog(Context context, int resourceId, string details = null)
        {
            ErrorDialog(context, context.Resources.GetText(resourceId), details);
        }

        public static void InfoDialog(Context context, int resourceId)
        {
            InfoDialog(context, context.Resources.GetText(resourceId));
        }

        public static void Toast(Context context, int resourceId)
        {
            Toast(context, context.Resources.GetText(resourceId));
        }


        public static void ErrorDialog(Context context, string message, string details = null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                using (var builder = new AlertDialog.Builder(context))
                {
                    builder.SetCancelable(false);
                    builder.SetTitle(context.Resources.GetText(Resource.String.Common_Error));
                    builder.SetMessage(message + " " + details);
                    builder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                    builder.SetPositiveButton("OK", (senderAlert, args) => { });
                    builder.Show();
                }
            });
        }

        public static void InfoDialog(Context context, string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                using (var builder = new AlertDialog.Builder(context))
                {
                    builder.SetCancelable(false);
                    builder.SetTitle(context.Resources.GetText(Resource.String.Common_Information));
                    builder.SetMessage(message);
                    builder.SetIcon(Android.Resource.Drawable.IcDialogInfo);
                    builder.SetPositiveButton("OK", (senderAlert, args) => { });
                    builder.Show();
                }
            });
        }

        public static void Toast(Context context, string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                new ShowToastRunnable(context, message).Run();
            });
        }
    }
}