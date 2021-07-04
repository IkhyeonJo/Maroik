using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyLaboratory.RequestWebAPI
{
    class Program
    {
        readonly static string url = "https://***REMOVED***:5001/";
        static async Task Main(string[] args)
        {
            try
            {
                await PostRequest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine();

            try
            {
                await GetRequest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }
        public static async Task PostRequest()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            dynamic dynamicJson = new ExpandoObject();
            dynamicJson.username = "admin".ToString();
            dynamicJson.password = "securePassword";
            string json = "";
            json = Newtonsoft.Json.JsonConvert.SerializeObject(dynamicJson);


            HttpClient client = new HttpClient(clientHandler);
            client.BaseAddress = new Uri($"{url}"); // MyLaboratory.WebAPI Server
            var response = await client.PostAsync("/api/Account/login", new StringContent(json, Encoding.UTF8, "application/json"));
            if (response != null)
            {
                Console.WriteLine(response.ToString());
                try
                {
                    string responJsonText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responJsonText);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static async Task GetRequest()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            Uri geturi = new Uri($"{url}WeatherForecast"); // MyLaboratory.WebAPI Server & Get
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(clientHandler);
            System.Net.Http.HttpResponseMessage responseGet = await client.GetAsync(geturi);
            string response = await responseGet.Content.ReadAsStringAsync();
            Console.WriteLine(response);
        }
    }
}
