using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace InsanePlugin
{
    public class RemoteJsonFile
    {
        private readonly string _url = string.Empty;

        public JObject Json { get; private set; } = null;

        public RemoteJsonFile(string url)
        {
            _url = url;
        }

        public void LoadAsync()
        {
            Task.Run(() =>
            {
                Load().Wait();
            });
        }

        private async Task Load()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = await client.GetStringAsync(_url);
                    Json = JObject.Parse(json);
                }
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"An error occurred while downloading {_url}\n{ex.Message}");
            }
        }
    }
}