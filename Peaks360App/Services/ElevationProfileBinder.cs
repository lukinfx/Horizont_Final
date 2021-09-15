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

namespace Peaks360App.Services
{
    public class ElevationProfileBinder : Binder, IElevationProfileService
    {
        public ElevationProfileService Service { get; private set; }

        public ElevationProfileBinder(ElevationProfileService service)
        {
            this.Service = service;
        }

        public void AddElevationProfile(float fromAngle, float toAngle, float precision)
        {
            Service.AddElevationProfile(fromAngle, toAngle, precision);
        }
    }
}