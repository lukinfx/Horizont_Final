using System;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
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
        [TestCase(18, 49, 110000, 0, 18.00, 49.99)]
        [TestCase(18, 49, 110000, 180, 18.00, 48.01)]
        [TestCase(18, 49, 80000, 90, 19.10, 49.00)]
        [TestCase(18, 49, 80000, -90, 16.90, 49.00)]
        [TestCase(18, 49, 80000, 60, 18.95, 49.36)]
        public void QuickGetGeoLocation(double inLon, double inLat, double distance, double angle, double expLon, double expLat)
        {
            GpsLocation a = new GpsLocation(inLon, inLat, 0);
            var b = GpsUtils.QuickGetGeoLocation(a, distance, angle);

            Assert.AreEqual(expLat, Math.Round(b.Latitude * 100) / 100);
            Assert.AreEqual(expLon, Math.Round(b.Longitude * 100) / 100);
        }
    }
}