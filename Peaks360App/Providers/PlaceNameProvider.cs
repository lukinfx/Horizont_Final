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
using System.Threading.Tasks;
using Peaks360Lib.Domain.Models;
using Xamarin.Essentials;

namespace Peaks360App.Providers
{
    public static class PlaceNameProvider
    {
        private static string Append(this string str, string separator, string param)
        {
            if (param == null)
                return str;
            if (!string.IsNullOrEmpty(str))
                str += separator;
            str += param;
            return str;
        }

        public static async Task<string> AsyncGetPlaceName(GpsLocation location)
        {
            //https://docs.microsoft.com/en-us/xamarin/essentials/geocoding?tabs=android

            try
            {
                var loc = new Xamarin.Essentials.Location(location.Latitude, location.Longitude);

                var placemarks = await Geocoding.GetPlacemarksAsync(loc);

                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    var geocodeAddress = placemark.Thoroughfare;
                    geocodeAddress = geocodeAddress.Append(" ", placemark.SubThoroughfare);
                    geocodeAddress = geocodeAddress.Append(", ",placemark.SubLocality);
                    geocodeAddress = geocodeAddress.Append(", ", placemark.Locality);
                    geocodeAddress = geocodeAddress.Append(", ", placemark.CountryCode);
                    return geocodeAddress;
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}