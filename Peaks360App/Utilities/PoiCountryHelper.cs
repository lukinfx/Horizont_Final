using System.Globalization;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class PoiCountryHelper
    {
        public static string GetCountryName(PoiCountry country)
        {
            switch (country)
            {
                case PoiCountry.AUT:
                    return "Austria";
                case PoiCountry.CZE:
                    return "Czech republic";
                case PoiCountry.FRA:
                    return "France";
                case PoiCountry.DEU:
                    return "Germany";
                case PoiCountry.HUN:
                    return "Hungary";
                case PoiCountry.ITA:
                    return "Italy";
                case PoiCountry.POL:
                    return "Poland";
                case PoiCountry.ROU:
                    return "Romania";
                case PoiCountry.SVK:
                    return "Slovakia";
                case PoiCountry.SVN:
                    return "Slovenia";
                case PoiCountry.ESP:
                    return "Spain";
                case PoiCountry.CHE:
                    return "Switzerland";
                default:
                    return "Unknown";
            }
        }

        public static PoiCountry? GetDefaultCountry()
        {
            var regionCode = RegionInfo.CurrentRegion;
            if (PoiCountry.TryParse<PoiCountry>(regionCode.ThreeLetterISORegionName, out var poiCountry))
            {
                return poiCountry;
            }

            return null;
        }

        public static Language GetDefaultLanguage()
        {
            var regionCode = RegionInfo.CurrentRegion;
            if (!PoiCountry.TryParse<PoiCountry>(regionCode.ThreeLetterISORegionName, out var poiCountry))
            {
                return Language.English;
            }

            switch (poiCountry)
            {
                case PoiCountry.AUT: //austria
                case PoiCountry.DEU: //germany
                case PoiCountry.CHE: //switzerland
                case PoiCountry.LIE: //liechtenstein
                    return Language.German;

                case PoiCountry.CZE: //czech
                case PoiCountry.SVK: //slovakia
                    return Language.Czech;

                case PoiCountry.FRA: //france
                    return Language.French;

                case PoiCountry.ITA: //italy
                case PoiCountry.MCO: //monaco
                    return Language.Italian;

                case PoiCountry.ESP: //spain
                case PoiCountry.PRT: //portugal + join pbf_files\azores
                case PoiCountry.AZO: //azores
                case PoiCountry.AND: //andorra
                    return Language.Spanish;

                case PoiCountry.GBR: //great-britain
                case PoiCountry.IRL: //ireland
                case PoiCountry.IMN: //isle-of-man
                    return Language.English;

                case PoiCountry.HUN: //hungary
                case PoiCountry.POL: //poland
                case PoiCountry.ROU: //romania
                case PoiCountry.SVN: //slovenia
                case PoiCountry.BEL: //belgium
                case PoiCountry.BIH: //bosna & Herzegovina
                case PoiCountry.HRV: //croatia
                case PoiCountry.BGR: //bulgaria
                case PoiCountry.DNK: //denmark
                case PoiCountry.FRO: //faroe-islands
                case PoiCountry.FIN: //finland
                case PoiCountry.LUX: //luxembourg
                case PoiCountry.NLD: //netherlands
                case PoiCountry.NOR: //norway
                case PoiCountry.ERB: //serbia
                case PoiCountry.SWE: //sweden
                case PoiCountry.GRC: //greece
                case PoiCountry.UKR: //ukraine
                case PoiCountry.BLR: //belarus
                case PoiCountry.ALB: //albania
                case PoiCountry.CYP: //cyprus
                case PoiCountry.EST: //estonia
                case PoiCountry.XKX: //kosovo
                case PoiCountry.LVA: //latvia
                case PoiCountry.LTU: //lithuania
                case PoiCountry.MKD: //macedonia
                case PoiCountry.MLT: //malta
                case PoiCountry.RUS: //russia
                case PoiCountry.GEO: //georgia
                case PoiCountry.GGY: //guernsey
                case PoiCountry.MNE: //montenegro
                case PoiCountry.MDA: //moldova
                    return Language.English;

                default:
                    return Language.English;
            }
        }
    }
}