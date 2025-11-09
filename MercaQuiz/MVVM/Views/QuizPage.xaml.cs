using MercaQuiz.Helpers;
using MercaQuiz.MVVM.ViewModels;
using MercaQuiz.MVVM.Models;

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
        if (!BackendUtility.TryGetInt(query, "n", out var n)) n =30;

        // leggi parametro tipo (opzionale)
        TipoDomanda tipo = TipoDomanda.Nessuna;
        if (BackendUtility.TryGetInt(query, "tipo", out var tipoInt))
        {
            if (Enum.IsDefined(typeof(TipoDomanda), tipoInt))
                tipo = (TipoDomanda)tipoInt;
        }

        await _vm.LoadAsync(materiaId, n, tipo);
    }

 
}