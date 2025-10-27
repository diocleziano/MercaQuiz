using MercaQuiz.Helpers;
using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.MVVM.Views;


public partial class QuizPage : ContentPage, IQueryAttributable
{
    private readonly QuizViewModel _vm;

    public QuizPage(QuizViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!BackendUtility.TryGetInt(query, "materiaId", out var materiaId))
        {
            await Shell.Current.DisplayAlert("Errore", "materiaId mancante", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        if (!BackendUtility.TryGetInt(query, "n", out var n)) n = 30;
        await _vm.LoadAsync(materiaId, n);
    }

 
}