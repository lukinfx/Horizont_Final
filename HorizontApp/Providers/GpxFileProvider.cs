using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace HorizontApp.Providers
{
    public class GpxFileProvider
    {
        public static string GetFile()
        {
            var client = new HttpClient();
            var response = client.GetAsync("http://vrcholky.8u.cz/hory%20(3).gpx").Result;
            var xml = response.Content.ReadAsStringAsync().Result;
            return xml;
        }
        //Fetch GPX file by HttpClient
        //return GPX files as string 
        //
    }
}