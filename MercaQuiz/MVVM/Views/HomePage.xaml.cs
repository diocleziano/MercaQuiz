using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.MVVM.Views;

public partial class HomePage : ContentPage
{
    private readonly MaterieViewModel _vm;

    public HomePage(MaterieViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CaricaAsync();
    }
}