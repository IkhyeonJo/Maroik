using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyLaboratory.RequestRESTful
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            //var student = "{'username':'admin','password':'securePassword'}";
            dynamic dynamicJson = new ExpandoObject();
            dynamicJson.username = "admin".ToString();
            dynamicJson.password = "securePassword";
            string json = "";
            json = Newtonsoft.Json.JsonConvert.SerializeObject(dynamicJson);


            HttpClient client = new HttpClient(clientHandler);
            //client.BaseAddress = new Uri("http://localhost:5001/");
            client.BaseAddress = new Uri("https://***REMOVED***:5001/");
            var response = await client.PostAsync("/api/Account/login", new StringContent(json, Encoding.UTF8, "application/json"));
            if (response != null)
            {
                Console.WriteLine(response.ToString());
                string responJsonText = await response.Content.ReadAsStringAsync();
            }

            await GetRequest();
        }

        public static async Task GetRequest()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            Uri geturi = new Uri("https://***REMOVED***:5001/WeatherForecast"); //replace your url  
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(clientHandler);
            System.Net.Http.HttpResponseMessage responseGet = await client.GetAsync(geturi);
            string response = await responseGet.Content.ReadAsStringAsync();
        }
    }
}
