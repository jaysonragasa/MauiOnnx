using ViewModels;

namespace MauiOnnxAiTooling
{
    public partial class MainPage : ContentPage
    {
        public MainPage(AIChatViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

		protected override void OnAppearing()
		{
			base.OnAppearing();

            ((AIChatViewModel)BindingContext).AutoLoadModelAsync();
		}
    }
}
