using System;
using System.Collections.Generic;
using System.Linq;

namespace HorizontApp.Utilities
{
    public class HeadingStabilizator
    {
        public Queue<double> headings = new Queue<double>();

        public void AddValue(double value)
        {
            headings.Enqueue(value);

            //while (headings.Count() > 5) { headings.Dequeue(); } 
            if (headings.Count() > 5)
                headings.Dequeue();
        }

        public double GetHeading()
        {
            if (!headings.Any())
            {
                return 0;
            }

            var a = headings.Average();
            var q = new Queue<double>(10);

            if (Math.Abs(headings.Min() - headings.Max()) > 180)
            {
                var items = headings.ToList();

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] > 180)
                        items[i] = items[i] - 360;
                }

                a = items.Average();
            }

            if (a < 0)
                a = 360 + a;
            return a;
        }
    }
}