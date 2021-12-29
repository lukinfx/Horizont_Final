using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.DrawerLayout.Widget;
using Peaks360App.AppContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peaks360App.Views
{
    public class SlidingMenuControl: DrawerLayout
    {
        public SlidingMenuControl(Context context) : base(context)
        {
            AppContextLiveData.Instance.SetLocale(context);
        }
        public SlidingMenuControl(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            AppContextLiveData.Instance.SetLocale(context);
        }
        public SlidingMenuControl(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            AppContextLiveData.Instance.SetLocale(context);
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            return base.OnInterceptTouchEvent(ev);
        }

        public bool IsMenuOpened()
        {
            return IsDrawerOpen((int)GravityFlags.Start);
        }

        public void OpenMenu()
        {
            OpenDrawer((int) GravityFlags.Start);
        }

        public void CloseMenu()
        {
            CloseDrawer((int)GravityFlags.Start);
        }
    }
}