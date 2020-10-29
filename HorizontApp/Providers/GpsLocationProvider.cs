using System;
using HorizontLib.Domain.Models;
using Xamarin.Essentials;

namespace HorizontApp.Providers
{
    public class GpsLocationProvider
    {
        private GpsLocation currentLocation;
        public GpsLocation CurrentLocation { get { return currentLocation; } }

        public GpsLocationProvider()
        {
            currentLocation = new GpsLocation();
        }

        public void Start()
        { 
            //nothing to do
        }

        public async System.Threading.Tasks.Task<GpsLocation> GetLocationAsync()
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
                    return currentLocation;
                }
                return null;

                //Celadna-Pstruzi
                /*currentLocation.Latitude = 49.5651525;
                currentLocation.Longitude = 18.3406403;
                currentLocation.Altitude = 430;
                return currentLocation;*/

                //Svarna hanka
                /*currentLocation.Latitude = 49.4894558;
                currentLocation.Longitude = 18.4914856;
                currentLocation.Altitude = 830;
                return currentLocation;*/
            }
            catch (FeatureNotSupportedException ex)
            {
                throw new Exception($"GPS is not supported. {ex.Message}");
            }
            catch (FeatureNotEnabledException ex)
            {
                throw new Exception($"GPS is not enabled. {ex.Message}");
            }
            catch (PermissionException ex)
            {
                throw new Exception($"GPS is not allowed. {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when fetching GPS location. {ex.Message}");
            }
        }

        /*public GpsLocation GetLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var getLocationrequest = Geolocation.GetLocationAsync(request);
                getLocationrequest.Wait();

                Location location = getLocationrequest.Result;

                if (location != null)
                {
                    currentLocation.Latitude = location.Latitude;
                    currentLocation.Longitude = location.Longitude;
                    currentLocation.Altitude = location.Altitude.Value;
                    return currentLocation;
                }
                return null;
            }
            catch (FeatureNotSupportedException ex)
            {
                throw new Exception($"GPS is not supported. {ex.Message}");
            }
            catch (FeatureNotEnabledException ex)
            {
                throw new Exception($"GPS is not enabled. {ex.Message}");
            }
            catch (PermissionException ex)
            {
                throw new Exception($"GPS is not allowed. {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when fetching GPS location. {ex.Message}");
            }
        }*/
    }
}