using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MercaQuiz.Data.Repository;
using MercaQuiz.Messaging;
using MercaQuiz.MVVM.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace MercaQuiz.MVVM.ViewModels;
public partial class AddEditMateriaViewModel : ObservableObject
{
    private readonly MaterieRepository _repo;
    private readonly DomandeQuizRepository _domandeRepo;   
    private readonly IMessenger _messenger;

    // Stato
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private int materiaId;

    // Campi base
    [ObservableProperty] private string nome = string.Empty;
    [ObservableProperty] private string? codice;
    [ObservableProperty] private string facolta = string.Empty;
    [ObservableProperty] private int annoAccademico; // 0..3 (o 1..3, decidi tu)
    [ObservableProperty] private string? docente;
    [ObservableProperty] private int? crediti;
    [ObservableProperty] private string? coloreHex;   // #RRGGBB
    [ObservableProperty] private string? icona;       // emoji o risorsa
    [ObservableProperty] private string? note;
    [ObservableProperty] private bool isArchiviata;

    // Nuovi campi
    [ObservableProperty] private DateTime? dataProssimoEsame; // opzionale
    [ObservableProperty] private bool isSuperato;
    [ObservableProperty] private int numeroTentativi;
    [ObservableProperty] private int votoEsame;
    [ObservableProperty] private int numeroQuizEffettuati;
    [ObservableProperty] private int numeroQuizSuperati;
    [ObservableProperty] private int numeroQuizFalliti;

    // Helper per gestire la visibilità del DatePicker (campo opzionale)
    [ObservableProperty] private bool hasDataProssimoEsame;

    [ObservableProperty] private ObservableCollection<DomandaQuiz> domande = new(); 
    [ObservableProperty] private int domandeCount;                                  

    public AddEditMateriaViewModel(MaterieRepository repo, DomandeQuizRepository domandeRepo, IMessenger? messenger = null)
    {
        _repo = repo;
        _domandeRepo = domandeRepo;
        _messenger = messenger ?? WeakReferenceMessenger.Default;
    }

    public async Task LoadAsync(int id)
    {
        if (id <= 0) { IsEditMode = false; MateriaId = 0; return; }

        var all = await _repo.GetAllAsync();
        var m = all.FirstOrDefault(x => x.Id == id);
        if (m is null)
        {
            await Shell.Current.DisplayAlert("Errore", "Materia non trovata.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        IsEditMode = true;
        MateriaId = m.Id;

        // base
        Nome = m.Nome;
        Codice = m.Codice;
        Facolta = m.Facolta;
        AnnoAccademico = m.AnnoAccademico;
        Docente = m.Docente;
        Crediti = m.Crediti;
        ColoreHex = m.ColoreHex;
        Icona = m.Icona;
        Note = m.Note;
        IsArchiviata = m.IsArchiviata;

        // nuovi
        DataProssimoEsame = m.DataProssimoEsame;
        HasDataProssimoEsame = m.DataProssimoEsame.HasValue;
        IsSuperato = m.IsSuperato;
        NumeroTentativi = m.NumeroTentativi;
        VotoEsame = m.VotoEsame;
        NumeroQuizEffettuati = m.NumeroQuizEffettuati;
        NumeroQuizSuperati = m.NumeroQuizSuperati;
        NumeroQuizFalliti = m.NumeroQuizFalliti;

        await LoadDomandeAsync();
    }

    private async Task LoadDomandeAsync()
    {
        var list = await _domandeRepo.GetByMateriaIdAsync(MateriaId);
        Domande = new ObservableCollection<DomandaQuiz>(list.OrderBy(d => d.Id));
        DomandeCount = Domande.Count;
    }

    [RelayCommand]
    private async Task AggiungiDomandaAsync()
    {
        await Shell.Current.GoToAsync($"domanda-edit?materiaId={MateriaId}");
    }

    [RelayCommand]
    private async Task ModificaDomandaAsync(DomandaQuiz? domanda)
    {
        if (domanda is null) return;
        await Shell.Current.GoToAsync($"domanda-edit?materiaId={MateriaId}&domandaId={domanda.Id}");
    }

    [RelayCommand]
    private async Task EliminaDomandaAsync(DomandaQuiz? domanda)
    {
        if (domanda is null) return;
        var ok = await Shell.Current.DisplayAlert("Conferma", "Eliminare questa domanda?", "Elimina", "Annulla");
        if (!ok) return;

        await _domandeRepo.DeleteAsync(domanda);
        await LoadDomandeAsync();
    }

    [RelayCommand]
    private async Task SalvaAsync()
    {
        // Validazioni minime
        if (string.IsNullOrWhiteSpace(Nome))
        {
            await Shell.Current.DisplayAlert("Dati mancanti", "Il nome è obbligatorio.", "OK");
            return;
        }
        if (!string.IsNullOrWhiteSpace(ColoreHex) &&
            !Regex.IsMatch(ColoreHex.Trim(), "^#([0-9A-Fa-f]{6})$"))
        {
            await Shell.Current.DisplayAlert("Colore non valido", "Usa il formato #RRGGBB.", "OK");
            return;
        }
        if (NumeroTentativi < 0 || NumeroQuizEffettuati < 0 || NumeroQuizSuperati < 0 || NumeroQuizFalliti < 0)
        {
            await Shell.Current.DisplayAlert("Valori non validi", "I numeri non possono essere negativi.", "OK");
            return;
        }
        if (VotoEsame < 0 || VotoEsame > 31) // metti il range che usi (0..30 o 18..30 e 31=30L)
        {
            await Shell.Current.DisplayAlert("Voto non valido", "Il voto deve essere tra 0 e 31.", "OK");
            return;
        }

        var now = DateTime.UtcNow;
        var dataEsame = HasDataProssimoEsame ? DataProssimoEsame : null;

        if (!IsEditMode)
        {
            var m = new Materia
            {
                Nome = Nome.Trim(),
                Codice = string.IsNullOrWhiteSpace(Codice) ? null : Codice.Trim(),
                Facolta = Facolta,
                AnnoAccademico = AnnoAccademico,
                Docente = string.IsNullOrWhiteSpace(Docente) ? null : Docente.Trim(),
                Crediti = Crediti,
                ColoreHex = string.IsNullOrWhiteSpace(ColoreHex) ? null : ColoreHex.Trim(),
                Icona = string.IsNullOrWhiteSpace(Icona) ? null : Icona.Trim(),
                Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
                IsArchiviata = IsArchiviata,
                // nuovi
                DataProssimoEsame = dataEsame,
                IsSuperato = IsSuperato,
                NumeroTentativi = NumeroTentativi,
                VotoEsame = VotoEsame,
                NumeroQuizEffettuati = NumeroQuizEffettuati,
                NumeroQuizSuperati = NumeroQuizSuperati,
                NumeroQuizFalliti = NumeroQuizFalliti,
                // timestamps
                CreatoIlUtc = now,
                AggiornatoIlUtc = now
            };
            await _repo.InsertAsync(m);
        }
        else
        {
            var list = await _repo.GetAllAsync();
            var m = list.First(x => x.Id == MateriaId);

            m.Nome = Nome.Trim();
            m.Codice = string.IsNullOrWhiteSpace(Codice) ? null : Codice.Trim();
            m.Facolta = Facolta;
            m.AnnoAccademico = AnnoAccademico;
            m.Docente = string.IsNullOrWhiteSpace(Docente) ? null : Docente.Trim();
            m.Crediti = Crediti;
            m.ColoreHex = string.IsNullOrWhiteSpace(ColoreHex) ? null : ColoreHex.Trim();
            m.Icona = string.IsNullOrWhiteSpace(Icona) ? null : Icona.Trim();
            m.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            m.IsArchiviata = IsArchiviata;

            // nuovi
            m.DataProssimoEsame = dataEsame;
            m.IsSuperato = IsSuperato;
            m.NumeroTentativi = NumeroTentativi;
            m.VotoEsame = VotoEsame;
            m.NumeroQuizEffettuati = NumeroQuizEffettuati;
            m.NumeroQuizSuperati = NumeroQuizSuperati;
            m.NumeroQuizFalliti = NumeroQuizFalliti;

            m.AggiornatoIlUtc = now;

            await _repo.UpdateAsync(m);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AnnullaAsync() => await Shell.Current.GoToAsync("..");


}
