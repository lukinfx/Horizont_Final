using Peaks360Lib.Domain.Models;

namespace Peaks360App.AppContext
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

        public AppContextStaticData(GpsLocation myLocation, PlaceInfo myLocationPlaceInfo, double heading) : base()
        {
            this.myLocation = myLocation;
            this.myLocationPlaceInfo = new PlaceInfo(myLocationPlaceInfo.PlaceName, myLocationPlaceInfo.Country);
            Heading = heading;
        }

        public override void Start()
        {
            //Nothing to do here
        }
    }
}