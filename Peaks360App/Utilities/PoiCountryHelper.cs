using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.Content;
using Android.Content.Res;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Utilities
{
    public class PoiCountryHelper
    {
        private static List<PoiCountry> _countryList;
        public static List<PoiCountry> GetAllCountries()
        {
            if (_countryList == null)
            {
                _countryList = new List<PoiCountry>();
                var countryValue = Enum.GetValues(typeof(PoiCountry));
                foreach (PoiCountry value in countryValue)
                {
                    _countryList.Add(value);
                }
            }

            return _countryList;
        }

        public static string GetCountryName(PoiCountry? country)
        {
            if (country == null)
            {
                return "";
            }

            switch (country)
            {
                case PoiCountry.AUT: return "Austria";
                case PoiCountry.CZE: return "Czech republic";
                case PoiCountry.FRA: return "France";
                case PoiCountry.DEU: return "Germany";
                case PoiCountry.HUN: return "Hungary";
                case PoiCountry.ITA: return "Italy";
                case PoiCountry.POL: return "Poland";
                case PoiCountry.ROU: return "Romania";
                case PoiCountry.SVK: return "Slovakia";
                case PoiCountry.SVN: return "Slovenia";
                case PoiCountry.ESP: return "Spain";
                case PoiCountry.CHE: return "Switzerland";
                case PoiCountry.BEL: return "Belgium";
                case PoiCountry.BIH: return "Bosna & Herzegovina";
                case PoiCountry.HRV: return "Croatia";
                case PoiCountry.BGR: return "Bulgaria";
                case PoiCountry.DNK: return "Denmark";
                case PoiCountry.FIN: return "Finland";
                case PoiCountry.LIE: return "Liechtenstein";
                case PoiCountry.LUX: return "Luxembourg";
                case PoiCountry.NLD: return "Netherlands";
                case PoiCountry.NOR: return "Norway";
                case PoiCountry.ERB: return "Serbia";
                case PoiCountry.SWE: return "Sweden";
                case PoiCountry.GRC: return "Greece";
                case PoiCountry.UKR: return "Ukraine";
                case PoiCountry.BLR: return "Belarus";
                case PoiCountry.ALB: return "Albania";
                case PoiCountry.CYP: return "Cyprus";
                case PoiCountry.EST: return "Estonia";
                case PoiCountry.GBR: return "Great Britain";
                case PoiCountry.IRL: return "Ireland";
                case PoiCountry.XKX: return "Kosovo";
                case PoiCountry.LVA: return "Latvia";
                case PoiCountry.LTU: return "Lithuania";
                case PoiCountry.MKD: return "Macedonia";
                case PoiCountry.MLT: return "Malta";
                case PoiCountry.MCO: return "Monaco";
                case PoiCountry.PRT: return "Portugal";
                case PoiCountry.RUS: return "Russia";
                case PoiCountry.AND: return "Andorra";
                case PoiCountry.FRO: return "Faroe Islands";
                case PoiCountry.GEO: return "Georgia";
                case PoiCountry.GGY: return "Guernsey & Jersey";
                case PoiCountry.IMN: return "Isle of man";
                case PoiCountry.MNE: return "Montenegro";
                case PoiCountry.AZO: return "Azores";
                case PoiCountry.MDA: return "Moldova";
                default: return "Unknown";
            }
        }

        public static PoiCountry? GetCountry(string countryCode)
        {
            var ri = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(x => !x.Equals(CultureInfo.InvariantCulture)) //Remove the invariant culture as a region cannot be created from it.
                .Where(x => !x.IsNeutralCulture) //Remove nuetral cultures as a region cannot be created from them.
                .Select(x => new RegionInfo(x.LCID))
                .SingleOrDefault(x => x.TwoLetterISORegionName.Equals(countryCode, StringComparison.InvariantCulture));

            Enum.TryParse<PoiCountry>(ri?.ThreeLetterISORegionName, out var poiCountry);
            return poiCountry;
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

        public static List<string> GetLanguageNames()
        {
            var result = new List<string>();
            foreach (var language in (Language[])Enum.GetValues(typeof(Language)))
            {
                result.Add(GetLanguageName(language));
            }

            return result;
        }

        public static string GetLanguageName(Language language)
        {
            switch (language)
            {
                case Language.English:
                    return "English";
                case Language.German:
                    return "Deutsche";
                case Language.Czech:
                    return "Čeština";
                case Language.French:
                    return "Français";
                case Language.Italian:
                    return "Italino";
                case Language.Spanish:
                    return "Español";
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }

        public static Language GetLanguageCode(string languageName)
        {
            switch (languageName)
            {
                case "English":
                    return Language.English;
                case "Deutsche":
                    return Language.German;
                case "Čeština":
                    return Language.Czech;
                case "Français":
                    return Language.French;
                case "Italino":
                    return Language.Italian;
                case "Español":
                    return Language.Spanish;
                default:
                    throw new ArgumentOutOfRangeException(nameof(languageName), languageName, null);
            }
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
                case PoiCountry.PRT: //portugal
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

        public static void SetLocale(Resources resources, Language language)
        {

            switch (language)
            {
                case Language.English:
                    resources.Configuration.SetLocale(new Java.Util.Locale("en"));
                    break;
                case Language.German:
                    resources.Configuration.SetLocale(new Java.Util.Locale("de"));
                    break;
                case Language.Czech:
                    resources.Configuration.SetLocale(new Java.Util.Locale("cz"));
                    break;
                case Language.French:
                    resources.Configuration.SetLocale(new Java.Util.Locale("fr"));
                    break;
                case Language.Italian:
                    resources.Configuration.SetLocale(new Java.Util.Locale("it"));
                    break;
                case Language.Spanish:
                    resources.Configuration.SetLocale(new Java.Util.Locale("es"));
                    break;
            }

            resources.UpdateConfiguration(resources.Configuration, resources.DisplayMetrics);
        }

        public static int GetCountryIcon(PoiCountry country)
        {
            //Icons from https://www.countryflags.com/en/icons-overview/
            switch (country)
            {
                case PoiCountry.AUT:
                    return Resource.Drawable.flag_of_Austria;
                case PoiCountry.CZE:
                    return Resource.Drawable.flag_of_CzechRepublic;
                case PoiCountry.FRA:
                    return Resource.Drawable.flag_of_France;
                case PoiCountry.DEU:
                    return Resource.Drawable.flag_of_Germany;
                case PoiCountry.HUN:
                    return Resource.Drawable.flag_of_Hungary;
                case PoiCountry.ITA:
                    return Resource.Drawable.flag_of_Italy;
                case PoiCountry.POL:
                    return Resource.Drawable.flag_of_Poland;
                case PoiCountry.ROU:
                    return Resource.Drawable.flag_of_Romania;
                case PoiCountry.SVK:
                    return Resource.Drawable.flag_of_Slovakia;
                case PoiCountry.SVN:
                    return Resource.Drawable.flag_of_Slovenia;
                case PoiCountry.ESP:
                    return Resource.Drawable.flag_of_Spain;
                case PoiCountry.CHE:
                    return Resource.Drawable.flag_of_Switzerland;
                case PoiCountry.BEL:
                    return Resource.Drawable.flag_of_Belgium;
                case PoiCountry.BIH:
                    return Resource.Drawable.flag_of_BosniaHerzegovina;
                case PoiCountry.HRV:
                    return Resource.Drawable.flag_of_Croatia;
                case PoiCountry.BGR:
                    return Resource.Drawable.flag_of_Bulgaria;
                case PoiCountry.DNK:
                    return Resource.Drawable.flag_of_Denmark;
                case PoiCountry.FIN:
                    return Resource.Drawable.flag_of_Finland;
                case PoiCountry.LIE:
                    return Resource.Drawable.flag_of_Liechtenstein;
                case PoiCountry.LUX:
                    return Resource.Drawable.flag_of_Luxembourg;
                case PoiCountry.NLD:
                    return Resource.Drawable.flag_of_Netherlands;
                case PoiCountry.NOR:
                    return Resource.Drawable.flag_of_Norway;
                case PoiCountry.ERB:
                    return Resource.Drawable.flag_of_Serbia;
                case PoiCountry.SWE:
                    return Resource.Drawable.flag_of_Sweden;
                case PoiCountry.GRC:
                    return Resource.Drawable.flag_of_Greece;
                case PoiCountry.UKR:
                    return Resource.Drawable.flag_of_Ukraine;
                case PoiCountry.BLR:
                    return Resource.Drawable.flag_of_Belarus;
                case PoiCountry.ALB:
                    return Resource.Drawable.flag_of_Albania;
                case PoiCountry.CYP:
                    return Resource.Drawable.flag_of_Cyprus;
                case PoiCountry.EST:
                    return Resource.Drawable.flag_of_Estonia;
                case PoiCountry.GBR:
                    return Resource.Drawable.flag_of_UnitedKingdom;
                case PoiCountry.XKX:
                    return Resource.Drawable.flag_of_Kosovo;
                case PoiCountry.LVA:
                    return Resource.Drawable.flag_of_Latvia;
                case PoiCountry.LTU:
                    return Resource.Drawable.flag_of_Lithuania;
                case PoiCountry.MLT:
                    return Resource.Drawable.flag_of_Malta;
                case PoiCountry.MCO:
                    return Resource.Drawable.flag_of_Monaco;
                case PoiCountry.PRT:
                    return Resource.Drawable.flag_of_Portugal;
                case PoiCountry.RUS:
                    return Resource.Drawable.flag_of_Russia;
                case PoiCountry.AND:
                    return Resource.Drawable.flag_of_Andorra;
                case PoiCountry.FRO:
                    return Resource.Drawable.flag_of_FaroeIslands;
                case PoiCountry.GEO:
                    return Resource.Drawable.flag_of_Georgia;
                case PoiCountry.MNE:
                    return Resource.Drawable.flag_of_Montenegro;
                case PoiCountry.MDA:
                    return Resource.Drawable.flag_of_Moldova;
                case PoiCountry.IRL:
                    return Resource.Drawable.flag_of_Ireland;
                case PoiCountry.MKD:
                    return Resource.Drawable.flag_of_Macedonia;
                case PoiCountry.GGY:
                    return Resource.Drawable.flag_of_Guernsey;
                case PoiCountry.IMN:
                    return Resource.Drawable.flag_of_IsleOfMan;
                case PoiCountry.AZO:
                    return Resource.Drawable.flag_of_Azores;
                default:
                    return Resource.Drawable.flag_of_Unknown;
            }

        }
    }
}