using Android.App;
using Android.Content;

namespace Peaks360App.Utilities
{
    public class PopupHelper
    {

        public static void ErrorDialog(Context context, string message, string details = null)
        {
            using (var builder = new AlertDialog.Builder(context))
            {
                builder.SetTitle(context.Resources.GetText(Resource.String.Error));
                builder.SetMessage(message + " " + details);
                builder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                builder.SetPositiveButton("OK", (senderAlert, args) => { }); 
                builder.Show();
            }
        }

        public static void InfoDialog(Context context, string message)
        {
            using (var builder = new AlertDialog.Builder(context))
            {
                builder.SetTitle(context.Resources.GetText(Resource.String.Information));
                builder.SetMessage(message);
                builder.SetIcon(Android.Resource.Drawable.IcDialogInfo);
                builder.Show();
                builder.SetPositiveButton("OK", (senderAlert, args) => { });
            }
        }
    }
}