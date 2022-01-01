using Peaks360Lib.Domain.Enums;

namespace Peaks360Lib.Domain.ViewModel
{
    public class PoiFilterItem
    {
        public PoiFilter PoiFilter { get; set; }
        public string Description { get; set; }

        public PoiFilterItem(PoiFilter poiFilter, string description)
        {
            PoiFilter = poiFilter;
            Description = description;
        }
    }
}
