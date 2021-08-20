using MyLaboratory.App.Common;
using MyLaboratory.App.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MyLaboratory.App.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string email;
        private string password;
        public string Email
        {
            get => email;
            set => SetProperty(ref email, value);
        }
        public string Password 
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        public Command LoginCommand { get; }

        public INavigation Navigation { get; set; }

        public LoginViewModel(INavigation navigation)
        {
            Navigation = navigation;
            LoginCommand = new Command(OnLoginClicked);
        }

        private async void OnLoginClicked(object obj)
        {
            try
            {
                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                };
                string json = JsonConvert.SerializeObject(new { username = Email, password = Password });


                HttpClient client = new HttpClient(clientHandler);
                client.BaseAddress = new Uri($"{ServerSetting.DomainName}"); // MyLaboratory.WebAPI Server
                HttpResponseMessage response = await client.PostAsync("/api/Account/login", new StringContent(json, Encoding.UTF8, "application/json"));
                if (response != null)
                {
                    try
                    {
                        string responJsonText = await response.Content.ReadAsStringAsync();
                        JWTManager.AccessToken = JObject.Parse(responJsonText)["accessToken"].ToString();
                        JWTManager.RefreshToken = JObject.Parse(responJsonText)["refreshToken"].ToString();
                        await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
                    }
                    catch (Exception e)
                    {
                        await Application.Current.MainPage.DisplayAlert("Exception occured", e.ToString(), "CANCEL");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Fail to login", "please check Email or Password", "CANCEL");
                }
            }
            catch (Exception e)
            {
                await Application.Current.MainPage.DisplayAlert("Exception occured", e.ToString(), "CANCEL");
            }
        }

        //public static async Task GetRequest()
        //{
        //    HttpClientHandler clientHandler = new HttpClientHandler();
        //    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

        //    Uri geturi = new Uri($"{url}/WeatherForecast"); // MyLaboratory.WebAPI Server & Get
        //    System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(clientHandler);
        //    System.Net.Http.HttpResponseMessage responseGet = await client.GetAsync(geturi);
        //    string response = await responseGet.Content.ReadAsStringAsync();
        //    Console.WriteLine(response);
        //}
        //public static async Task AuthorizationPostRequest()
        //{
        //    HttpClientHandler clientHandler = new HttpClientHandler();
        //    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        //    var loginRequest = new { refreshToken = refreshToken };
        //    string json = "";
        //    json = JsonConvert.SerializeObject(loginRequest);


        //    HttpClient client = new HttpClient(clientHandler);
        //    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        //    client.BaseAddress = new Uri($"{url}"); // MyLaboratory.WebAPI Server
        //    var response = await client.PostAsync("/api/Account/refresh-token", new StringContent(json, Encoding.UTF8, "application/json"));
        //    if (response != null)
        //    {
        //        Console.WriteLine(response.ToString());
        //        try
        //        {
        //            string responJsonText = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine(responJsonText);
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine(e);
        //        }
        //    }
        //}
    }
}
