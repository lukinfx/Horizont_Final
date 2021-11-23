using System;
using System.Linq;
using System.Collections.Generic;

namespace Peaks360App.Utilities.HeadingStabilizator
{
    public class QueuedHeadingStabilizator : IHeadingStabilizator
    {
        private const int QueueSize = 25;
        private class Item
        {
            public double Sin;
            public double Cos;
        }
        private Queue<Item> queuedItems = new Queue<Item>();

        public void AddValue(double value)
        {
            var sin = Math.Sin(Math.PI * value / 180);
            var cos = Math.Cos(Math.PI * value / 180);

            queuedItems.Enqueue(new Item() {Sin = sin, Cos = cos});

            if (queuedItems.Count() > QueueSize)
                queuedItems.Dequeue();
        }

        public double GetHeading()
        {
            if (!queuedItems.Any())
            {
                return 0;
            }

            var avgSin = queuedItems.Average( i => i.Sin);
            var avgCos = queuedItems.Average(i => i.Cos);

            var heading = (Math.Atan2(avgSin, avgCos) / Math.PI) * 180;
            return heading;
        }
    }
}