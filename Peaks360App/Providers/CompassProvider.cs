using Peaks360App.Utilities;
using Javax.Security.Auth;
using System;
using Android.Util;
using Peaks360App.AppContext;
using Peaks360App.Utilities.HeadingStabilizator;
using Xamarin.Essentials;

namespace Peaks360App.Providers
{
    public class CompassProvider
    {
        private static string TAG = "Horizon-CompassProvider";
        private IHeadingStabilizator _headingStabilizator = new QueuedHeadingStabilizator();

        public CompassProvider()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            Xamarin.Essentials.Compass.ReadingChanged += Compass_ReadingChanged;
        }
        
        public Action<double> OnHeadingChanged;

        public double Heading { get ; private set; }

        private int GetHeadingOfset()
        {
            switch (DeviceDisplay.MainDisplayInfo.Rotation)
            {
                case DisplayRotation.Rotation0: return 0;
                case DisplayRotation.Rotation90: return 90;
                case DisplayRotation.Rotation180: return 180;
                case DisplayRotation.Rotation270: return 270;
                default: return 0;
            }
        }

        void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var data = e.Reading;

            int headingOffset = GetHeadingOfset();
            var tmpHeading = (data.HeadingMagneticNorth + headingOffset) % 360;

            _headingStabilizator.AddValue(tmpHeading);
            var newHeading = _headingStabilizator.GetHeading();

            Log.WriteLine(LogPriority.Debug, TAG, $"ReadingChanged event: {data.HeadingMagneticNorth} + {headingOffset} = {tmpHeading}  (F:{newHeading})");

            if (Math.Abs(newHeading - Heading) > 0.2)
            {
                Heading = newHeading;
                OnHeadingChanged?.Invoke(Heading);
            }
        }

        public void Start()
        {
            try
            {
                if (Compass.IsMonitoring)
                    return;
                Compass.Start(SensorSpeed.UI, applyLowPassFilter: true);
            }
            catch (FeatureNotSupportedException)
            {
                //TODO: inform user that Compass is not supported on this device
                //throw new Exception($"Compass not supported. {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when starting compass. {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                if (!Compass.IsMonitoring)
                    return;

                Compass.Stop();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when stopping compass. {ex.Message}");
            }
        }
    }
}