using System;

namespace Peaks360App.Utilities.HeadingStabilizator
{
    public class SimpleHeadingStabilizator : IHeadingStabilizator
    {
        private double smoothingFactor = 0.95;
        private double lastSin;
        private double lastCos;

        public void AddValue(double value)
        {
            lastSin = smoothingFactor * lastSin + (1 - smoothingFactor) * Math.Sin(Math.PI * value / 180);
            lastCos = smoothingFactor * lastCos + (1 - smoothingFactor) * Math.Cos(Math.PI * value / 180);
        }

        public double GetHeading()
        {
            return (Math.Atan2(lastSin, lastCos) / Math.PI) * 180;
        }
    }
}