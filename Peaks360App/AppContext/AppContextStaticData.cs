using Peaks360Lib.Domain.Models;

namespace Peaks360App.AppContext
{
    public class AppContextStaticData : AppContextBase
    {
        private static AppContextStaticData _instance;
        public static IAppContext Instance { get { return _instance; } }

        public static AppContextStaticData GetInstance(GpsLocation myLocation, PlaceInfo myLocationPlaceInfo, double? heading)
        {
            _instance = new AppContextStaticData(myLocation, myLocationPlaceInfo, heading);
            return _instance;
        }

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

        private AppContextStaticData(GpsLocation myLocation, PlaceInfo myLocationPlaceInfo, double? heading) : base()
        {
            this.myLocation = myLocation;
            this.myLocationPlaceInfo = new PlaceInfo(myLocationPlaceInfo.PlaceName, myLocationPlaceInfo.Country);
            HeadingX = heading;
        }

        public override void Start()
        {
            //Nothing to do here
        }
    }
}