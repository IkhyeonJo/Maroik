using MyLaboratory.App.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace MyLaboratory.App.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}