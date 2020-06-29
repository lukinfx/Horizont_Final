using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

namespace HorizontApp.Providers
{
    class CompassProvider
    {
        private double heading;
        SensorSpeed speed = SensorSpeed.UI;
       
        public CompassProvider()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            Xamarin.Essentials.Compass.ReadingChanged += Compass_ReadingChanged;
        }

         public double Heading { get { return heading; } }

        void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var data = e.Reading;
            heading = (90 + data.HeadingMagneticNorth) % 360;
            // Process Heading Magnetic North
        }

        public void Start()
        {
            try
            {
                if (Compass.IsMonitoring)
                    return;

                Compass.Start(speed);
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