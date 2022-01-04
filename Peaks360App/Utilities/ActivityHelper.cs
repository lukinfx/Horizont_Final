using Android.App;
using Android.Views;

namespace Peaks360App.Utilities
{
    public class ActivityHelper
    {
        private static SystemUiFlags GetUiOptions()
        { 
            return SystemUiFlags.HideNavigation |
                   SystemUiFlags.LayoutHideNavigation |
                   SystemUiFlags.LayoutFullscreen |
                   SystemUiFlags.Fullscreen |
                   SystemUiFlags.LayoutStable |
                   SystemUiFlags.ImmersiveSticky;
        }

        public static void ChangeSystemUiVisibility(Dialog dialog, bool hasFocus = true)
        {
            dialog.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)GetUiOptions();
        }
        
        public static void ChangeSystemUiVisibility(Activity activity, bool hasFocus = true)
        {
            activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)GetUiOptions();
        }
    }
}