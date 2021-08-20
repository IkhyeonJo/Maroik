using MyLaboratory.App.Common;
using MyLaboratory.App.Services;
using MyLaboratory.App.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyLaboratory.App
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
        }

        protected async override void OnStart()
        {
            if (!string.IsNullOrEmpty(JWTManager.AccessToken))
            {
                await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
            }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
