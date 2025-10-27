using MercaQuiz.Data.Database;
using MercaQuiz.Helpers;

namespace MercaQuiz.MVVM.Views;

public partial class BootstrapPage : ContentPage
{
    private readonly IDatabaseService _db;
    private readonly AppShell _shell;

    // ⬇️ Unico costruttore: NESSUN costruttore vuoto!
    public BootstrapPage(IDatabaseService db, AppShell shell)
    {
        InitializeComponent();
        _db = db;
        _shell = shell;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _db.InitializeAsync();
            Application.Current.MainPage = _shell;
        }
        catch (Exception ex)
        {
#if DEBUG
            await DisplayAlert("Errore avvio", ex.Message, "OK");
#endif
            throw;
        }
    }
}