using System;
using System.Collections.Generic;
using System.Linq;

namespace Peaks360App.Utilities
{
    public class HeadingStabilizator
    {
        public Queue<double> headings = new Queue<double>();


        public void AddValue(double value)
        {
            headings.Enqueue(value);

            //while (headings.Count() > 5) { headings.Dequeue(); } 
            if (headings.Count() > 10)
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

            var items = headings.ToList();
            
            if (Math.Abs(headings.Min() - headings.Max()) > 180)
            {

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] > 180)
                        items[i] = items[i] - 360;
                }

                a = items.Average();
            }


            /*if (items.Count > 2)
            {
                if (Math.Abs(items[items.Count - 1] - items[items.Count - 2]) > 5)
                {
                    if (Math.Abs(items[items.Count - 1] - items[items.Count - 2]) > 180)
                    {
                        if (items[items.Count-1] > 180)
                        {
                            items[items.Count-1] -= 360;
                        }
                        else
                        {
                            items[items.Count - 2] -= 360;
                        }
                    }
                    a = (items[items.Count - 1] - items[items.Count - 2]) / 2;
                }
            }*/
            
                



            if (a < 0)
                a = 360 + a;
            return a;
        }

        
    }
}