using System;
using Android.App;
using Android.Runtime;
using HorizontApp.Utilities;

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
            CompassViewSettings.Instance().LoadData(this);
        }
    }
}