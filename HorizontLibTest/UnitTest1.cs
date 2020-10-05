using HorizontLib.Domain.Models;
using HorizontLib.Utilities;
using NUnit.Framework;

namespace HorizontLibTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            GpsLocation a = new GpsLocation(49,18,0);
            var b = GpsUtils.GetGeoLocation(a, 10, 0);


        }
    }
}