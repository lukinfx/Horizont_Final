using HorizontLib.Domain.Models;

namespace HorizontApp.AppContext
{
    public class AppContextStaticData : AppContextBase
    {
        public override float ViewAngleHorizontal
        {
            get
            {
                return Settings.AViewAngleHorizontal;
            }
        }

        public override float ViewAngleVertical
        {
            get
            {
                return Settings.AViewAngleVertical;
            }
        }

        public AppContextStaticData(GpsLocation myLocation, string myLocationName, double heading)
        {
            this.myLocation = myLocation;
            this.myLocationName = myLocationName;
            Heading = heading;
        }

        public override void Start()
        {
            //Nothing to do here
        }
    }
}