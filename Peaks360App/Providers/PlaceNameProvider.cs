﻿using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Models;

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

        public static async Task<PlaceInfo> AsyncGetPlaceName(GpsLocation location)
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

                    var country = PoiCountryHelper.GetCountry(placemark.CountryCode) ?? PoiCountryHelper.GetDefaultCountryByPhoneSettings();
                    return new PlaceInfo(geocodeAddress, country);
                }
                return new PlaceInfo();
            }
            catch
            {
                return new PlaceInfo();
            }
        }
    }
}