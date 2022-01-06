using System;
using System.Threading.Tasks;
using Peaks360App.AppContext;
using Peaks360App.Services;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Peaks360App.Providers
{
    public class GpsLocationProvider
    {
        private GpsLocation currentLocation;
        private ElevationTile _elevationTile;
        private bool waitingForResponse = false;
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
                if (AppContextLiveData.Instance.Settings.IsManualLocation)
                {
                    return AppContextLiveData.Instance.Settings.ManualLocation;
                }

                if (waitingForResponse)
                {
                    return null;
                }

                if (!DependencyService.Get<IGpsService>().IsGpsAvailable())
                {
                    return null;
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Best, new TimeSpan(0, 0, 0, 5));

                waitingForResponse = true;
                Location location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    currentLocation.Latitude = location.Latitude;
                    currentLocation.Longitude = location.Longitude;
                    currentLocation.Altitude = location.Altitude.Value;

                    if (AppContextLiveData.Instance.Settings.AltitudeFromElevationMap)
                    {
                        if (_elevationTile == null || !_elevationTile.HasElevation(currentLocation))
                        {
                            _elevationTile = null;
                            var et = new ElevationTile(currentLocation);
                            if (et.Exists())
                            {
                                if (et.LoadFromZip())
                                {
                                    _elevationTile = et;
                                }
                            }
                        }

                        if (_elevationTile != null)
                        {
                            currentLocation.Altitude = _elevationTile.GetElevation(currentLocation);
                        }
                    }

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
            finally
            {
                waitingForResponse = false;
            }
        }

        public static async Task<GpsLocation> GetLastKnownLocationAsync()
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location != null)
            {
                return Peaks360App.Utilities.GpsUtils.Convert(location);
            }

            return null;
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