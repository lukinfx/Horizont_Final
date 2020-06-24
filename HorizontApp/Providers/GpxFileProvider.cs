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
        public static string GetFile(string url)
        {
            var client = new HttpClient();
            var response = client.GetAsync(url).Result;
            var xml = response.Content.ReadAsStringAsync().Result;
            return xml;
        }
        //Fetch GPX file by HttpClient
        //return GPX files as string 
        //
    }
}