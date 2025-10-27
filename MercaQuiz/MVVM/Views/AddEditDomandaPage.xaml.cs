using MercaQuiz.Helpers;
using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.MVVM.Views;

public partial class AddEditDomandaPage : ContentPage, IQueryAttributable
{
    private readonly AddEditDomandaViewModel _vm;

    public AddEditDomandaPage(AddEditDomandaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!BackendUtility.TryGetInt(query, "materiaId", out var materiaId))
        {
            await Shell.Current.DisplayAlert("Errore", "Parametro materiaId mancante.", "OK");
            return;
        }

        if (BackendUtility.TryGetInt(query, "domandaId", out var domandaId))
        {
            await _vm.LoadForEditAsync(materiaId, domandaId);
        }
        else
        {
            _vm.InitForMateria(materiaId);
        }
    }

    
}