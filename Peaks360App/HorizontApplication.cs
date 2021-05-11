using System;
using Android.App;
using Android.Runtime;

namespace Peaks360App
{
    [Application]
    class Peaks360Application : Application
    {
        public Peaks360Application(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            //AppContextLiveData.Instance.Initialize(Context);
            //AppContextLiveData.Instance.SetLocale(this);
        }
    }
}