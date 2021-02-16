using Android.App;
using Android.Content;

namespace Peaks360App.Utilities
{
    public class PopupHelper
    {

        public static void ErrorDialog(Context context, string title, string message)
        {
            using (var builder = new AlertDialog.Builder(context))
            {
                builder.SetTitle(title);
                builder.SetMessage(message);
                builder.SetIcon(Android.Resource.Drawable.IcDialogAlert);
                builder.SetPositiveButton("OK", (senderAlert, args) => { }); 
                builder.Show();
            }
        }

        public static void InfoDialog(Context context, string title, string message)
        {
            using (var builder = new AlertDialog.Builder(context))
            {
                builder.SetTitle(title);
                builder.SetMessage(message);
                builder.SetIcon(Android.Resource.Drawable.IcDialogInfo);
                builder.Show();
                builder.SetPositiveButton("OK", (senderAlert, args) => { });
            }
        }
    }
}