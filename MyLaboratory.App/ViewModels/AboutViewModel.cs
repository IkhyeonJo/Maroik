using MyLaboratory.App.Common;
using System;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyLaboratory.App.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            Title = "About";
            //OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://aka.ms/xamarin-quickstart"));
            OpenWebCommand = new Command(OnAuthorizationPostRequestClicked);
        }

        public ICommand OpenWebCommand { get; }

        private async void OnAuthorizationPostRequestClicked(object obj)
        {
            try
            {
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                var loginRequest = new { refreshToken = JWTManager.RefreshToken };
                string json = "";
                json = Newtonsoft.Json.JsonConvert.SerializeObject(loginRequest);


                HttpClient client = new HttpClient(clientHandler);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {JWTManager.AccessToken}");
                client.BaseAddress = new Uri($"{App.AppSettings.DomainName}"); // MyLaboratory.WebAPI Server
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
            catch (Exception e)
            {
                await Application.Current.MainPage.DisplayAlert("Exception occured", e.ToString(), "CANCEL");
            }
        }
    }
}