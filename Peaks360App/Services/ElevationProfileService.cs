using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Util;

namespace Peaks360App.Services
{
    public interface IElevationProfileService
    {
        void AddElevationProfile(float fromAngle, float toAngle, float precision);
    }

    [Service(Name = "cz.fdevstudio.peaks360.ElevationProfileService")]
    public class ElevationProfileService : Service, IElevationProfileService
    {
        static readonly string TAG = typeof(ElevationProfileService).FullName;

        private IElevationProfileService _elevationProfileService;
        public IBinder Binder { get; private set; }

        public void AddElevationProfile(float fromAngle, float toAngle, float precision)
        {
            _elevationProfileService.AddElevationProfile(fromAngle, toAngle, precision);
        }

        public override void OnCreate()
        {
            // This method is optional to implement
            base.OnCreate();
            Log.Debug(TAG, "OnCreate");
            _elevationProfileService = new ElevationProfileImpl();
        }

        public override IBinder OnBind(Intent intent)
        {
            // This method must always be implemented
            Log.Debug(TAG, "OnBind");
            this.Binder = new ElevationProfileBinder(this);
            return this.Binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            // This method is optional to implement
            Log.Debug(TAG, "OnUnbind");
            return base.OnUnbind(intent);
        }

        public override void OnDestroy()
        {
            // This method is optional to implement
            Log.Debug(TAG, "OnDestroy");
            Binder = null;
            _elevationProfileService = null;
            base.OnDestroy();
        }

    }
}