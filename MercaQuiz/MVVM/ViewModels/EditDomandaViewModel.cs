using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MercaQuiz.Data.Repository;
using MercaQuiz.Helpers;
using MercaQuiz.MVVM.Models;
using System.Collections.ObjectModel;

namespace MercaQuiz.MVVM.ViewModels;

public partial class EditDomandaViewModel: ObservableObject, IQueryAttributable
{
    private readonly DomandeQuizRepository _repo;

    // id passato via query
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string domanda = string.Empty;
    [ObservableProperty] private string answer1 = string.Empty;
    [ObservableProperty] private string answer2 = string.Empty;
    [ObservableProperty] private string answer3 = string.Empty;
    [ObservableProperty] private string answer4 = string.Empty;

    [ObservableProperty] private string moduloAppartenzaText = string.Empty;
    // Sempre 4 risposte in UI
    public ObservableCollection<string> Risposte { get; } =
        new ObservableCollection<string>(new[] { "", "", "", "" });

    // Picker: mostra 1..4 ma SelectedIndex è 0..3
    public List<string> PickerItems { get; } = new() { "1", "2", "3", "4" };

    [ObservableProperty]
    private int correctAnswerIndex; // 0..3

    private DomandaQuiz? _model; // originale dal DB

    public List<string> TipoItems { get; } = new() { "Domanda Quiz", "Domanda Fine Lezione" };

    [ObservableProperty]
    private int selectedTipoIndex = 0; // 0 -> DomandaQuiz, 1 -> DomandaFineLezione

    public TipoDomanda SelectedTipo => (TipoDomanda)Math.Clamp(SelectedTipoIndex + 1, 1, 2);

    partial void OnSelectedTipoIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFineLezione));
    }

    public bool IsFineLezione => SelectedTipo == TipoDomanda.DomandaFineLezione;


    public EditDomandaViewModel(DomandeQuizRepository repo)
    {
        _repo = repo;
    }

    // Riceve ?id=123 da Shell
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BackendUtility.TryGetInt(query, "domandaId", out var domandaId))
        {
            await LoadAsync(domandaId);
        }
      
    }

    private async Task LoadAsync(int id)
    {
        Id = id;
        _model = await _repo.GetByIdAsync(id);
        if (_model == null)
        {
            await Shell.Current.DisplayAlert("Errore", "Domanda non trovata.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        Domanda = _model.Domanda ?? string.Empty;
        ModuloAppartenzaText = _model.ModuloAppartenenza ?? string.Empty;
        SelectedTipoIndex = (int)_model.TipologiaDomanda - 1;
        
        Answer1 = _model.Risposte.ElementAtOrDefault(0) ?? string.Empty;
        Answer2 = _model.Risposte.ElementAtOrDefault(1) ?? string.Empty;
        Answer3 = _model.Risposte.ElementAtOrDefault(2) ?? string.Empty;
        Answer4 = _model.Risposte.ElementAtOrDefault(3) ?? string.Empty;


        var list = _model.Risposte?.ToList() ?? new List<string>();
        while (list.Count < 4) list.Add(string.Empty);
        for (int i = 0; i < 4; i++)
        {
            if (i < Risposte.Count) Risposte[i] = list[i];
            else Risposte.Add(list[i]);
        }

        CorrectAnswerIndex = Math.Clamp(_model.RispostaCorretta, 0, 3);
    }

    [RelayCommand]
    private async Task Save()
    {
        _model = await _repo.GetByIdAsync(Id);
        if (_model == null)
        {
            await Shell.Current.DisplayAlert("Errore", "Domanda non trovata.", "OK");
            return;
        }

        // Validazione semplice
        var answers = new List<string?>
        {
            string.IsNullOrWhiteSpace(Answer1) ? null : Answer1,
            string.IsNullOrWhiteSpace(Answer2) ? null : Answer2,
            string.IsNullOrWhiteSpace(Answer3) ? null : Answer3,
            string.IsNullOrWhiteSpace(Answer4) ? null : Answer4,
        }
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s!.Trim())
        .ToList();


        var trimmed = Risposte.Select(r => (r ?? string.Empty).Trim()).ToList();
        if (string.IsNullOrWhiteSpace(Domanda))
        {
            await Shell.Current.DisplayAlert("Validazione", "La domanda è obbligatoria.", "OK");
            return;
        }
        if (trimmed.Any(string.IsNullOrWhiteSpace) || trimmed.Count != 4)
        {
            await Shell.Current.DisplayAlert("Validazione", "Inserisci tutte e 4 le risposte.", "OK");
            return;
        }
        if (CorrectAnswerIndex < 0 || CorrectAnswerIndex > 3)
        {
            await Shell.Current.DisplayAlert("Validazione", "Seleziona la risposta corretta (1–4).", "OK");
            return;
        }


        _model.TipologiaDomanda = SelectedTipo;
        _model.ModuloAppartenenza = ModuloAppartenzaText.Trim();
        _model.Domanda = Domanda.Trim();
        _model.Risposte = answers;
        _model.RispostaCorretta = CorrectAnswerIndex;
        _model.AggiornatoIlUtc = DateTime.Now;
        try
        {
            _model.Validate();
            await _repo.UpdateAsync(_model); // firma reale senza ct
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Errore salvataggio", ex.Message, "OK");
            return;
        }
        await Shell.Current.GoToAsync("..");

    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}
