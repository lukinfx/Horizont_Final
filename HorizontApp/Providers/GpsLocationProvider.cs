using System;
using HorizontApp.Domain.Models;
using Xamarin.Essentials;

namespace HorizontApp.Providers
{
    public class GpsLocationProvider
    {
        private GpsLocation currentLocation;
        public GpsLocation CurrentLocation { get { return currentLocation; } }

        public async System.Threading.Tasks.Task<Location> GetLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                Location location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    currentLocation.Latitude = location.Latitude;
                    currentLocation.Longitude = location.Longitude;
                    currentLocation.Altitude = location.Altitude.Value;
                    return location;
                }
                return null;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
                return null;
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
                return null;
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
                return null;
            }
            catch (Exception ex)
            {
                // Unable to get location
                return null;
            }
        }
    }
}