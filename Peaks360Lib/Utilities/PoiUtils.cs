using System.Globalization;
using System.Linq;
using System.Text;

namespace Peaks360Lib.Utilities
{
    public static class PoiUtils
    {
        public static string RemoveDiacritics(this string str)
        {
            if (null == str) return null;
            var chars =
                from c in str.Normalize(NormalizationForm.FormD).ToCharArray()
                let uc = CharUnicodeInfo.GetUnicodeCategory(c)
                where uc != UnicodeCategory.NonSpacingMark
                select c;

            var cleanStr = new string(chars.ToArray()).Normalize(NormalizationForm.FormC);

            return cleanStr;
        }

        // or, alternatively
        public static string RemoveDiacritics2(this string str)
        {
            if (null == str) return null;
            var chars = str
                .Normalize(NormalizationForm.FormD)
                .ToCharArray()
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC);
        }
    }
}
