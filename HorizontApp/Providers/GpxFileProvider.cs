using System.Net.Http;

namespace HorizontApp.Providers
{
    public class GpxFileProvider
    {
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