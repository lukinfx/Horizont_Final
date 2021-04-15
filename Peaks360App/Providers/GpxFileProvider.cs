using System;
using System.Net.Http;
using System.Threading.Tasks;
using Java.Lang;

namespace Peaks360App.Providers
{
    public class GpxFileProvider
    {

        private static readonly string WebsiteUrl = "http://peaks360.hys.cz/horizont/";
        private static readonly string IndexFile = "peaks360-index.json";

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

        public async static Task GetFile(string url, Action<string> onFinished)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            Thread.Sleep(3000);
            var xml = await response.Content.ReadAsStringAsync();
            onFinished(xml);
        }

        //Fetch GPX file by HttpClient
        //return GPX files as string 
        //
    }
}