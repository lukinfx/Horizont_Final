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
using Peaks360App.Activities;

namespace Peaks360App.Services
{

    public class ElevationProfileServiceConnection : Java.Lang.Object, IServiceConnection, IElevationProfileService
    {
        static readonly string TAG = typeof(ElevationProfileServiceConnection).FullName;

        PhotoShowActivity photoShowActivity;
        public ElevationProfileServiceConnection(PhotoShowActivity activity)
        {
            IsConnected = false;
            Binder = null;
            photoShowActivity = activity;
        }

        public bool IsConnected { get; private set; }
        public ElevationProfileBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as ElevationProfileBinder;
            IsConnected = this.Binder != null;

            string message = "onServiceConnected - ";
            Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

            if (IsConnected)
            {
                message = message + " bound to service " + name.ClassName;
                //mainActivity.UpdateUiForBoundService();
            }
            else
            {
                message = message + " not bound to service " + name.ClassName;
                //mainActivity.UpdateUiForUnboundService();
            }

            Log.Info(TAG, message);
            //mainActivity.timestampMessageTextView.Text = message;

        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
            IsConnected = false;
            Binder = null;
            //mainActivity.UpdateUiForUnboundService();
        }

        public void AddElevationProfile(float fromAngle, float toAngle, float precision)
        {
            if (!IsConnected)
            {
                return;
            }

            Binder?.AddElevationProfile(fromAngle, toAngle, precision);
        }
    }
}