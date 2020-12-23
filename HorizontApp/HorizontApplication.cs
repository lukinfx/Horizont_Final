using System;
using Android.App;
using Android.Runtime;
using HorizontApp.AppContext;

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
            AppContextLiveData.Instance.Initialize(Context);
            AppContextLiveData.Instance.SetLocale(this);
        }
    }
}