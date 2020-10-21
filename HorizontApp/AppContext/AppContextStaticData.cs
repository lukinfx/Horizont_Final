﻿using HorizontLib.Domain.Models;

namespace HorizontApp.AppContext
{
    public class AppContextStaticData : AppContextBase
    {
        public AppContextStaticData(GpsLocation myLocation, double heading)
        {
            this.myLocation = myLocation;
            Heading = heading;
        }

        public override void Start()
        {
            //Nothing to do here
        }
    }
}