using Peaks360App.Utilities;
using Javax.Security.Auth;
using System;
using Xamarin.Essentials;

namespace Peaks360App.Providers
{
    public class CompassProvider
    {
        SensorSpeed speed = SensorSpeed.Default;


        private HeadingStabilizator2 _headingStabilizator = new HeadingStabilizator2();

        public CompassProvider()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            Xamarin.Essentials.Compass.ReadingChanged += Compass_ReadingChanged;
        }
        
        public Action<double> OnHeadingChanged;

        public double Heading { get ; private set; }

        void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var data = e.Reading;
            var tmpHeading = (90 + data.HeadingMagneticNorth) % 360;

            _headingStabilizator.AddValue(tmpHeading);
            var newHeading = _headingStabilizator.GetHeading();
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
                Compass.Start(speed, applyLowPassFilter: true);
            }
            catch (FeatureNotSupportedException ex)
            {
                throw new Exception($"Compass not supported. {ex.Message}");
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