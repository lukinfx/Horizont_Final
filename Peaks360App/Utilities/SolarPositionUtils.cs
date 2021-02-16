using System;

namespace Peaks360App.Utilities
{
    class SolarPositionUtils
    {
        private double _solarAzimut;
        public double SolarAzimuth
        {
            get { return _solarAzimut; }
            set { }
        }
        private double longitude = 48;
        private double latitude = 17;
        private double timeZone = 2;


        public static double _calculateSolarPosition(double longitude, double latitude, double timeZone)
        {
            DateTime now = DateTime.Now;
            var offset = now.Kind;
            double fractionalYear = ((2 * Math.PI) / 365) * (now.DayOfYear - 1 + ((now.Hour - 12) / 24));
            double declination = 0.006918 - 0.399912 * Math.Cos(fractionalYear) + 0.070257 * Math.Sin(fractionalYear) - 0.006758 * Math.Cos(2 * fractionalYear) + 0.000907 * Math.Sin(2 * fractionalYear) - 0.002697 * Math.Cos(3 * fractionalYear) + 0.00148 * Math.Sin(3 * fractionalYear);
            double time_offset =  + 4 * longitude - 60 * timeZone;
            double tst = now.Hour * 60 + now.Minute + now.Second / 60 + time_offset;
            double ha = (tst / 4) - 180;
            double zenith = Math.Acos(Math.Sin(latitude) * Math.Sin(declination)) + (Math.Cos(latitude) * Math.Cos(declination) * Math.Cos(ha));
            double azimuth = Math.Acos(-((Math.Sin(latitude) * Math.Cos(zenith)) - Math.Sin(declination)) / (Math.Cos(latitude) * Math.Sin(zenith)));
            double SolarAzimuth = 180 - azimuth;
            return SolarAzimuth;
        }
    }
}