using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MercaQuiz.Data.Repository;
using MercaQuiz.MVVM.Models;
using MercaQuiz.MVVM.Views;
using System.Collections.ObjectModel;

namespace MercaQuiz.MVVM.ViewModels;

public partial class MateriaDettaglioViewModel : ObservableObject
{
    private readonly MaterieRepository _materieRepo;
    private readonly DomandeQuizRepository _domandeRepo;

    private readonly IMessenger _messenger;

    [ObservableProperty] private Materia? record;
    [ObservableProperty] private int domandeCount;
    [ObservableProperty] private ObservableCollection<DomandaQuiz> domande = new();
    [ObservableProperty] private int numeroDomandeRichieste = 30;   // ⬅️ default 30


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



    private async Task LoadDomandeAsync(TipoDomanda tipoDomanda = TipoDomanda.Nessuna)               // ⬅️
    {
        List<DomandaQuiz> list = null;
        var tutte = await _domandeRepo.GetByMateriaIdAsync(MateriaId);

        if (tipoDomanda == TipoDomanda.Nessuna)
        {
            list = tutte;
        }
        else if(tipoDomanda == TipoDomanda.DomandaFineLezione)
        {
            var soloTipologia = tutte.Where(x => x.TipologiaDomanda == tipoDomanda);
            List<DomandaQuiz> soloTipologiaOrdinataPerModulo = soloTipologia
                .OrderBy(x => x.ModuloAppartenenza)
                .ThenBy(x =>
                {
                    // prende la parte prima del primo punto, prova a fare parse in int
                    var first = (x.Domanda ?? string.Empty).Split(new[] { '.' }, 2)[0].Trim();
                    if (int.TryParse(first, out var n))
                        return (0, n); // primo campo 0 = ha numero, secondo il valore numerico
                    return (1, int.MaxValue); // primo campo 1 = non numerico => viene dopo i numerici
                })
                .ThenBy(x => x.Domanda, StringComparer.OrdinalIgnoreCase)
                .ToList();
            list = soloTipologiaOrdinataPerModulo;
        }
        else if (tipoDomanda == TipoDomanda.DomandaQuiz)
        {
            list = tutte.Where(x => x.TipologiaDomanda == tipoDomanda).ToList();
        }

        Domande = new ObservableCollection<DomandaQuiz>(list);
        DomandeCount = Domande.Count;
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

        await Shell.Current.GoToAsync($"quiz?materiaId={MateriaId}&n={n}");
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
}




// Messaggio opzionale per refresh dopo salvataggio domanda
public sealed class DomandaChangedMessage : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<int>
{
    // value = materiaId toccato
    public DomandaChangedMessage(int materiaId) : base(materiaId) { }
}
