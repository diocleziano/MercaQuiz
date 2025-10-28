using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MercaQuiz.Config;
using System.Threading.Tasks;

namespace MercaQuiz.MVVM.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel()
    {
        // Carica valori correnti
        BasePath = AppConfigService.Current.BasePath;
        DbFileName = AppConfigService.Current.DbFileName;
    }

    [ObservableProperty]
    private string basePath = string.Empty;

    [ObservableProperty]
    private string dbFileName = string.Empty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        AppConfigService.Current.BasePath = BasePath?.Trim() ?? string.Empty;
        AppConfigService.Current.DbFileName = string.IsNullOrWhiteSpace(DbFileName) ? "MercaQuiz.db3" : DbFileName.Trim();
        AppConfigService.SaveCurrent();

        // Conferma all'utente
        await Shell.Current.DisplayAlert("Impostazioni", "Percorso database salvato. Riavvia l'app per applicare il nuovo DB.", "OK");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        // Ricarica valori dal file di config (annulla)
        BasePath = AppConfigService.Current.BasePath;
        DbFileName = AppConfigService.Current.DbFileName;
        await Task.CompletedTask;
    }
}
