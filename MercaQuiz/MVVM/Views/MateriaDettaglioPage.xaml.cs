using MercaQuiz.MVVM.ViewModels;

namespace MercaQuiz.MVVM.Views;

public partial class MateriaDettaglioPage : ContentPage, IQueryAttributable
{
    private readonly MateriaDettaglioViewModel _vm;

    public MateriaDettaglioPage(MateriaDettaglioViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("materiaId", out var raw) && raw is string s && int.TryParse(s, out var id))
        {
            _vm.SetMateriaId(id);
            await _vm.CaricaAsync();   // ← fondamentale
        }
        else
        {
            await Shell.Current.DisplayAlert("Errore", "Parametro materiaId mancante.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }
}