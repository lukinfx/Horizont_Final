using System.Net.Http;

namespace Peaks360App.Providers
{
    public class GpxFileProvider
    {

        private static readonly string WebsiteUrl = "http://horizon360.hys.cz/horizont/";
        private static readonly string IndexFile = "horizon-index.json";

        public static string GetIndexUrl()
        {
            return GetUrl(IndexFile);
        }

        public static string GetUrl(string path)
        {
            return WebsiteUrl + path;
        }

        /// <summary>
        /// Gets content from given URL
        /// </summary>
        /// <param name="url">URL location</param>
        /// <returns>Content of the file</returns>
        public static string GetFile(string url)
        {
            var client = new HttpClient();
            var response = client.GetAsync(url).Result;
            var xml = response.Content.ReadAsStringAsync().Result;
            return xml;
        }
        //Fetch GPX file by HttpClient
        //return GPX files as string 
        //
    }
}