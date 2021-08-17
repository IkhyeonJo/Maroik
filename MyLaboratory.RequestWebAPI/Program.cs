using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyLaboratory.RequestWebAPI
{
    class Program
    {
        readonly static string url = "https://localhost:5001";
        static string accessToken = "";
        static string refreshToken = "";
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

            Console.WriteLine();

            try
            {
                await AuthorizationPostRequest();
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
            var loginRequest = new { username = "admin".ToString(), password = "securePassword" };
            string json = "";
            json = Newtonsoft.Json.JsonConvert.SerializeObject(loginRequest);


            HttpClient client = new HttpClient(clientHandler);
            client.BaseAddress = new Uri($"{url}"); // MyLaboratory.WebAPI Server
            var response = await client.PostAsync("/api/Account/login", new StringContent(json, Encoding.UTF8, "application/json"));
            if (response != null)
            {
                Console.WriteLine(response.ToString());
                try
                {
                    string responJsonText = await response.Content.ReadAsStringAsync();
                    accessToken = JObject.Parse(responJsonText)["accessToken"].ToString();
                    refreshToken = JObject.Parse(responJsonText)["refreshToken"].ToString();
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

            Uri geturi = new Uri($"{url}/WeatherForecast"); // MyLaboratory.WebAPI Server & Get
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(clientHandler);
            System.Net.Http.HttpResponseMessage responseGet = await client.GetAsync(geturi);
            string response = await responseGet.Content.ReadAsStringAsync();
            Console.WriteLine(response);
        }

        public static async Task AuthorizationPostRequest()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            var loginRequest = new { refreshToken = refreshToken };
            string json = "";
            json = Newtonsoft.Json.JsonConvert.SerializeObject(loginRequest);


            HttpClient client = new HttpClient(clientHandler);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            client.BaseAddress = new Uri($"{url}"); // MyLaboratory.WebAPI Server
            var response = await client.PostAsync("/api/Account/refresh-token", new StringContent(json, Encoding.UTF8, "application/json"));
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
    }

    public class LoginRequest
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
