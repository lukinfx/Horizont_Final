using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace HorizontApp.Utilities
{
    public class ExceptionHelper
    {
        public static string Exception2ErrorMessage(Exception ex)
        {
            //TODO: Use also ex.InnerException
            //ex.Message + ex.InnerException.Message + ex.InnerException.InnerException.Message ...
            return ex.Message;
        }
    }
}