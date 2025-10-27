using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.MVVM.Views;

public partial class EditDomandaPage : ContentPage
{
	public EditDomandaPage(EditDomandaViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm; // DI consigliata
    }
}