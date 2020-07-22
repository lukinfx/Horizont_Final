using System.Net.Http;

namespace HorizontApp.Providers
{
    public class GpxFileProvider
    {
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