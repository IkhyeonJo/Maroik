using MyLaboratory.App.Common;
using MyLaboratory.App.Configuration;
using MyLaboratory.App.Services;
using MyLaboratory.App.Views;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyLaboratory.App
{
    public partial class App : Application
    {

        private static AppSettings appSettings;
        public static AppSettings AppSettings
        {
            get
            {
                if (appSettings == null)
                {
                    LoadAppSettings();
                }
                return appSettings;
            }
        }

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

        private static void LoadAppSettings()
        {
#if DEBUG
            var appSettingsResourceStream = Assembly.GetAssembly(typeof(AppSettings)).GetManifestResourceStream("MyLaboratory.App.Configuration.appsettings.debug.json");
#else
            var appSettingsResourceStream = Assembly.GetAssembly(typeof(AppSettings)).GetManifestResourceStream("MyLaboratory.App.Configuration.appsettings.release.json");
#endif
            if (appSettingsResourceStream == null)
                return;

            using (var streamReader = new StreamReader(appSettingsResourceStream))
            {
                var jsonString = streamReader.ReadToEnd();
                appSettings = JsonConvert.DeserializeObject<AppSettings>(jsonString);
            }
        }
    }
}
