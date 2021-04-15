using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;

namespace MakeIndex
{
    class Program
    {
        private static string GetCategoryPart(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Mountains: return "00";
                case PoiCategory.Lakes: return "04";
                case PoiCategory.ViewTowers: return "03";
                case PoiCategory.Transmitters: return "02";
                case PoiCategory.Churches: return "06";
                case PoiCategory.Historic: return "05";
                case PoiCategory.Cities: return "01";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        private static string GetCountryPart(PoiCountry country)
        {
            switch (country)
            {
                case PoiCountry.AUT: return "3B46C567-83D0-4751-B296-BD6184229D";
                case PoiCountry.CZE: return "948268dd-71a7-4696-bb8c-dafdad5e48"; 
                case PoiCountry.FRA: return "9fa3dd4e-46bf-4a58-bbb8-625ea4755f";
                case PoiCountry.DEU: return "47FCF987-5655-4AE0-9964-367A88E546"; 
                case PoiCountry.HUN: return "fcf08d5b-060a-4379-a7d4-14d60e0787";
                case PoiCountry.ITA: return "4DBF1E0D-146C-4E12-99CD-BF47C424AA";
                case PoiCountry.POL: return "827EDFA8-83EA-4D7D-B18C-67518D08DB";
                case PoiCountry.ROU: return "3E337A9B-27BD-401B-8D53-4021445671";
                case PoiCountry.SVK: return "a936b146-ab96-451f-b24b-f270c8373e";
                case PoiCountry.SVN: return "DEABB987-AA20-46B7-A0C3-9CA94BA034";
                case PoiCountry.ESP: return "627c8265-793d-4450-8071-690d54ad0a";
                case PoiCountry.CHE: return "e6963288-eb9d-4e2b-bdc6-40d36ddc13";
                case PoiCountry.BEL: return "e2e2d4dc-43cc-467d-8a8c-81eebe325d";
                case PoiCountry.BIH: return "15fe4786-87dd-46a7-a1b5-6eb7d75a7a";
                case PoiCountry.HRV: return "959737df-a344-4284-b9c9-aab1e56b8c";
                case PoiCountry.BGR: return "ee791400-6414-472f-a0fe-86bfd820ed";
                case PoiCountry.DNK: return "72598c7a-1910-4a0c-972b-0ea56cd6fd";
                case PoiCountry.FIN: return "2d88f613-f0d8-4565-a585-2783ee5f8d";
                case PoiCountry.LIE: return "7dcb0c3a-c928-4ac7-84aa-13bb162fc1";
                case PoiCountry.LUX: return "9bd3a549-5db5-4801-ae7d-06b5ed98b2";
                case PoiCountry.NLD: return "2c5505c5-d40a-44d7-952d-4c1f02e218";
                case PoiCountry.NOR: return "2823c60d-10d7-4c1d-a184-6a1ac2766c";
                case PoiCountry.ERB: return "82a1781d-4612-4916-ba9a-ba7548543a";
                case PoiCountry.SWE: return "4b3be5ce-b7fe-4229-b8b0-164b8161ce";
                case PoiCountry.GRC: return "8cb0e465-3bc8-4072-aa10-5f3af2e1a2";
                case PoiCountry.UKR: return "00410178-37de-4caf-9bce-9b836db533";
                case PoiCountry.BLR: return "c420bbc6-40e5-43fb-b588-35b5a01ebd";
                case PoiCountry.ALB: return "60497641-f19f-4bee-93c3-1a750ff887";
                case PoiCountry.CYP: return "95b21b0f-d7f9-4c9d-8da2-02a5fee8f2";
                case PoiCountry.EST: return "4da59190-6d02-4796-97f4-8ed8ec1677";
                case PoiCountry.GBR: return "fb280934-9e32-47c9-86d5-f1bc1e63e7";
                case PoiCountry.IRL: return "8572effc-8344-4ea3-b1b5-4ad9d7a9c0";
                case PoiCountry.XKX: return "bd8721c8-6c90-4c22-9b96-85e99d293e";
                case PoiCountry.LVA: return "b5ec9c53-31fd-46dc-a2da-db3696a54e";
                case PoiCountry.LTU: return "0a58aeae-a523-4c68-9eae-ff2f60109d";
                case PoiCountry.MKD: return "0b910f9b-4cbb-45dc-aac4-a85631efe8";
                case PoiCountry.MLT: return "fb9ddc61-89e9-461f-bc97-d490a3fa43";
                case PoiCountry.MCO: return "20e1578a-a397-4e07-8bee-83a7ed6afc";
                case PoiCountry.PRT: return "127d8a08-469b-4e4c-81b4-1cf5f65b10";
                case PoiCountry.RUS: return "ba390b0c-ea41-4607-9f70-f0734cc9ea";
                case PoiCountry.AND: return "7d4b40c3-1d86-4a77-93b1-9dba42271c";
                case PoiCountry.FRO: return "a26512c9-9231-4219-a83f-1b0716154a";
                case PoiCountry.GEO: return "8bd0a102-9652-4b83-91b7-aeff3511be";
                case PoiCountry.GGY: return "dcce3793-0d88-4433-8a23-8f1c1b8e2d";
                case PoiCountry.IMN: return "416c870b-e80c-4e5e-8812-d95946cea8";
                case PoiCountry.MNE: return "c42b2519-caec-43ab-b897-34804c6bc2";
                case PoiCountry.AZO: return "f0491f1b-1b9b-4499-bb52-964da0e2a4";
                case PoiCountry.MDA: return "a7fb494b-510d-44c5-bbc0-71a3983ceb";
                default:
                    throw new ArgumentOutOfRangeException(nameof(country), country, null);
            }
        }

        private static Guid GetGuid(PoiCountry country, PoiCategory category)
        {
            return new Guid(GetCountryPart(country) + GetCategoryPart(category));
        }

        private static string GetFileName(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Mountains:
                    return "peaks.gpx";
                case PoiCategory.Lakes:
                    return "lakes.gpx";
                case PoiCategory.ViewTowers:
                    return "viewtowers.gpx";
                case PoiCategory.Transmitters:
                    return "transmitters.gpx";
                case PoiCategory.Churches:
                    return "churches.gpx";
                case PoiCategory.Historic:
                    return "historic.gpx";
                case PoiCategory.Cities:
                    return "settlements.gpx";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
        private static PoiData GetPoiData(string dir, PoiCategory category, PoiCountry country)
        {
            try
            {
                var filename = $"{dir}\\{GetFileName(category)}";
                var xml = File.ReadAllText(filename);
                var listOfPoi = GpxFileParser.Parse(xml, category, country, Guid.NewGuid());
                var count = listOfPoi.Count;
                //DateTime creation = File.GetCreationTime(filename);
                DateTime modificationDate = File.GetLastWriteTime(filename);

                var pd = new PoiData()
                {
                    Category = category,
                    DateCreated = modificationDate,
                    PointCount = count,
                    Url = $"PoiData/{country}/{GetFileName(category)}",
                    Description = "",
                    Id = GetGuid(country, category)
                };

                return pd;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        static void Main(string[] args)
        {
            var srcDir = args[0];

            var dirs = from dir in Directory.EnumerateDirectories(srcDir) select dir;

            var hi = new HorizonIndex();

            foreach (var dir in dirs)
            {
                var countryName = dir.Substring(dir.Length - 3);

                if (!Enum.TryParse<PoiCountry>(countryName, out var poiCountry))
                {
                    Console.WriteLine($"Invalid country name: {dir}");
                    continue;
                }

                var cd = new CountryData() {Country = poiCountry, PoiData = new List<PoiData>()};

                AddPoiData(cd, dir, PoiCategory.Mountains, poiCountry);
                AddPoiData(cd, dir, PoiCategory.Lakes, poiCountry);
                AddPoiData(cd, dir, PoiCategory.Cities, poiCountry);
                AddPoiData(cd, dir, PoiCategory.Historic, poiCountry);
                AddPoiData(cd, dir, PoiCategory.ViewTowers, poiCountry);
                AddPoiData(cd, dir, PoiCategory.Transmitters, poiCountry);
                AddPoiData(cd, dir, PoiCategory.Churches, poiCountry);

                hi.Add(cd);
            }

            var indexAsString = JsonConvert.SerializeObject(hi, new Newtonsoft.Json.Converters.StringEnumConverter());
            File.WriteAllText($"{srcDir}\\peaks360-index.json", indexAsString);

        }

        private static void AddPoiData(CountryData cd, string dir, PoiCategory category, PoiCountry poiCountry)
        {
            var pd = GetPoiData(dir, category, poiCountry);
            if (pd != null && pd.PointCount > 0)
            {
                cd.PoiData.Add(pd); 
            }
        }
    }
}
