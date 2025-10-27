using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.MVVM.Views;

public partial class AddEditMateriaPage : ContentPage, IQueryAttributable
{
    private readonly AddEditMateriaViewModel _vm;

    public AddEditMateriaPage(AddEditMateriaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Riceve "materiaId" dalla Shell (se presente) e carica i dati
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("materiaId", out var raw) && raw is string s && int.TryParse(s, out var id))
        {
            await _vm.LoadAsync(id);
        }
        else
        {
            await _vm.LoadAsync(0); // modalità nuovo
        }
    }
}