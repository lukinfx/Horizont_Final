using HorizontApp.Utilities;
using Javax.Security.Auth;
using System;
using Xamarin.Essentials;

namespace HorizontApp.Providers
{
    public class CompassProvider
    {
        SensorSpeed speed = SensorSpeed.UI;


        private HeadingStabilizator _headingStabilizator = new HeadingStabilizator();

        public CompassProvider()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            Xamarin.Essentials.Compass.ReadingChanged += Compass_ReadingChanged;
        }

        public double Heading { get ; private set; }

        void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var data = e.Reading;
            var tmpHeading = (90 + data.HeadingMagneticNorth) % 360;

            _headingStabilizator.AddValue(tmpHeading);
            // Process Heading Magnetic North

            Heading = _headingStabilizator.GetHeading();
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