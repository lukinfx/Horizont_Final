using System;
using Android.App;
using Android.Runtime;
using HorizontApp.Utilities;
using AppContext = HorizontApp.Utilities.AppContext;

namespace HorizontApp
{
    [Application]
    class HorizontApplication : Application
    {
        public HorizontApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            AppContext.Instance.Settings.LoadData(this);
        }
    }
}