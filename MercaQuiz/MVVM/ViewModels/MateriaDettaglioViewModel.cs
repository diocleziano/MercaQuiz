using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MercaQuiz.Data.Repository;
using MercaQuiz.MVVM.Models;
using MercaQuiz.MVVM.Views;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MercaQuiz.MVVM.ViewModels;

public partial class MateriaDettaglioViewModel : ObservableObject
{
    private readonly MaterieRepository _materieRepo;
    private readonly DomandeQuizRepository _domandeRepo;

    private readonly IMessenger _messenger;

    [ObservableProperty] private Materia? record;
    [ObservableProperty] private int domandeCount;
    // Domande visibili nella UI (filtered)
    [ObservableProperty] private ObservableCollection<DomandaQuiz> domande = new();
    [ObservableProperty] private int numeroDomandeRichieste = 30;   // ⬅️ default 30

    // Tipologia picker per filtro quiz
    public List<string> TipologiaItems { get; } = new() { "Tutte", "Quiz", "Domande Fine Lezione" };

    // selected index for the picker (0 = Tutte,1 = Quiz,2 = Domande Fine Lezione)
    private int _selectedTipologiaIndex =0;
    public int SelectedTipologiaIndex
    {
        get => _selectedTipologiaIndex;
        set
        {
            if (SetProperty(ref _selectedTipologiaIndex, value))
            {
                // no immediate action here; value will be used when starting the quiz
            }
        }
    }

    // Nuova proprietà di ricerca -- al cambiare applica il filtro
    [ObservableProperty] private string searchText = string.Empty;

    // Proprietà IsLoading: implementata esplicitamente per assicurare la notifica
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
                OnPropertyChanged(nameof(IsNotLoading)); ;
        }
    }

    public bool IsNotLoading => !IsLoading;

    // Lista completa in memoria usata per il filtering
    private List<DomandaQuiz> _allDomande = new();

    public int MateriaId { get; private set; }

    public MateriaDettaglioViewModel(
        MaterieRepository materieRepo,
        DomandeQuizRepository domandeRepo,
        IMessenger? messenger = null)
    {
        _materieRepo = materieRepo;
        _domandeRepo = domandeRepo;
        _messenger = messenger ?? WeakReferenceMessenger.Default;

        // Se hai già un DomandaChangedMessage, ricarica al volo
        _messenger.Register<DomandaChangedMessage>(this, async (_, __) => await CaricaAsync());
    }

    public void SetMateriaId(int id) => MateriaId = id;

    [RelayCommand]
    public async Task CaricaAsync()
    {
        // Carico materia
        var tutte = await _materieRepo.GetAllAsync();
        Record = tutte.FirstOrDefault(m => m.Id == MateriaId);

        if (Record is null)
        {
            await Shell.Current.DisplayAlert("Errore", "Materia non trovata.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await LoadDomandeAsync();
    }

    [RelayCommand]
    public async Task LoadDomandeAsync(TipoDomanda tipoDomanda = TipoDomanda.Nessuna)
    {
        IsLoading = true;
        try
        {
            // Load all questions for this materia (no incremental paging)
            var tutte = await _domandeRepo.GetByMateriaIdAsync(MateriaId);

            List<DomandaQuiz> list;
            if (tipoDomanda == TipoDomanda.Nessuna)
            {
                list = tutte.OrderBy(x => x.Domanda).ToList();
            }
            else if (tipoDomanda == TipoDomanda.DomandaFineLezione)
            {
                var soloTipologia = tutte.Where(x => x.TipologiaDomanda == tipoDomanda);
                List<DomandaQuiz> soloTipologiaOrdinataPerModulo = soloTipologia
                    .OrderBy(x =>
                    {
                        var first = (x.ModuloAppartenenza ?? string.Empty).Split(new[] { '-' }, 2)[0].Trim();
                        if (int.TryParse(first, out var n))
                            return (0, n);
                        return (1, int.MaxValue);
                    })
                    .ThenBy(x =>
                    {
                        var secondo = (x.ModuloAppartenenza ?? string.Empty).Split(new[] { '-' }, 2)[1].Trim();
                        return secondo;
                    })
                    .ThenBy(x =>
                    {
                        var first = (x.Domanda ?? string.Empty).Split(new[] { '.' }, 2)[0].Trim();
                        if (int.TryParse(first, out var n))
                            return (0, n);
                        return (1, int.MaxValue);
                    })
                    .ThenBy(x => x.Domanda, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                list = soloTipologiaOrdinataPerModulo;
            }
            else // TipoDomanda.DomandaQuiz
            {
                list = tutte.Where(x => x.TipologiaDomanda == tipoDomanda).OrderBy(x => x.Domanda).ToList();
            }

            // salva la lista completa e applica il filtro (eventuale SearchText)
            _allDomande = list ?? new List<DomandaQuiz>();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Keep a no-op LoadMore to satisfy any XAML bindings to RemainingItemsThresholdReachedCommand
    [RelayCommand]
    private Task LoadMoreDomandeAsync()
    {
        // incremental loading disabled — all items are already loaded by LoadDomandeAsync
        return Task.CompletedTask;
    }

    // Applica il filtro su _allDomande e aggiorna Domande e DomandeCount
    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Domande = new ObservableCollection<DomandaQuiz>(_allDomande);
        }
        else
        {
            var q = SearchText.Trim();
            var matched = _allDomande.Where(d =>
                (!string.IsNullOrEmpty(d.Domanda) && d.Domanda.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(d.RisposteJson) && d.Risposte.Any(r => !string.IsNullOrEmpty(r) && r.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0))
            ).ToList();

            Domande = new ObservableCollection<DomandaQuiz>(matched);
        }

        DomandeCount = Domande?.Count ?? 0;
    }

    // Quando SearchText cambia, riapplica il filtro
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ResetConteggi()
    {
        await _domandeRepo.ResetConteggiAsync();
        await LoadDomandeAsync();
    }

    [RelayCommand]
    public async Task AggiungiDomandaAsync()
    {
        // Vai a pagina Add/Edit domanda passando il materiaId
        await Shell.Current.GoToAsync($"domanda-add?materiaId={MateriaId}");
    }
    [RelayCommand]
    public async Task ModificaDomandaAsync(DomandaQuiz? domanda) // ⬅️
    {
        if (domanda is null) return;
        await Shell.Current.GoToAsync($"domanda-edit?domandaId={domanda.Id}");
    }

    [RelayCommand]
    public async Task EliminaDomandaAsync(DomandaQuiz? domanda) // ⬅️
    {
        if (domanda is null) return;
        var ok = await Shell.Current.DisplayAlert("Conferma", "Eliminare questa domanda?", "Elimina", "Annulla");
        if (!ok) return;
        await _domandeRepo.DeleteAsync(domanda);
        await LoadDomandeAsync();
    }

    [RelayCommand]
    public async Task AvviaQuizAsync()
    {
        if (DomandeCount <= 0)
        {
            await Shell.Current.DisplayAlert("Nessuna domanda", "Questa materia non ha domande.", "OK");
            return;
        }

        // N valido (min 1, max DomandeCount)
        var n = Math.Max(1, Math.Min(NumeroDomandeRichieste, DomandeCount));

        // mappa selectedTipologiaIndex a TipoDomanda
        TipoDomanda tipo;
        switch (SelectedTipologiaIndex)
        {
            case 1: tipo = TipoDomanda.DomandaQuiz; break;
            case 2: tipo = TipoDomanda.DomandaFineLezione; break;
            default: tipo = TipoDomanda.Nessuna; break;
        }

        // passare tipo come parametro numerico (0= tutte,1=quiz,2=fine lezione)
        await Shell.Current.GoToAsync($"quiz?materiaId={MateriaId}&n={n}&tipo={(int)tipo}");
    }


    [RelayCommand]
    public async Task ShowDomandeQuiz()
    {
        await LoadDomandeAsync(TipoDomanda.DomandaQuiz);
    }

    [RelayCommand]
    public async Task ShowDomandeFineLezione()
    {
        await LoadDomandeAsync(TipoDomanda.DomandaFineLezione);
    }

    [RelayCommand]
    public async Task ShowTutteLeDomande()
    {
        await LoadDomandeAsync(TipoDomanda.Nessuna);
    }

    // comando per cancellare la ricerca
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ApplyFilter();
    }
}



// Messaggio opzionale per refresh dopo salvataggio domanda
public sealed class DomandaChangedMessage : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<int>
{
    // value = materiaId toccato
    public DomandaChangedMessage(int materiaId) : base(materiaId) { }
}
