using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MercaQuiz.Converters;
using MercaQuiz.Data.Repository;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using MercaQuiz.MVVM.Models;

namespace MercaQuiz.MVVM.ViewModels;


public partial class QuizOption : ObservableObject
{
    public int Index { get; set; }

    public int Indice { get => Index + 1; }

    public string Text { get; set; } = string.Empty;

    // back-reference alla domanda
    public QuizQuestionItem? Parent { get; set; }


}



public partial class QuizQuestionItem : ObservableObject
{
    public int QuestionId { get; set; }

    public int NumeroDomanda { get; set; }
    public string Testo { get; set; } = string.Empty;

    public string DomandaConNumero { get => $"{NumeroDomanda}: {Testo}"; }
    public ObservableCollection<QuizOption> Opzioni { get; set; } = new();
    public int CorrectIndex { get; set; }

    [ObservableProperty] private Color coloreRisposta = Colors.Transparent;

    public int RispostaCorretta => CorrectIndex + 1; //1-based per UI

    [ObservableProperty] private int selectedIndex = -1; // -1 = non risposto
}

public partial class QuizViewModel : ObservableObject
{
    private readonly DomandeQuizRepository _repo;
    private readonly Random _rng = new();

    public int MateriaId { get; private set; }
    public int N { get; private set; }

    [ObservableProperty] private bool showAnswers = false;
    [ObservableProperty] private ObservableCollection<QuizQuestionItem> domande = new();

    public QuizViewModel(DomandeQuizRepository repo)
    {
        _repo = repo;
    }

    // aggiunto parametro tipo per filtrare le domande
    public async Task LoadAsync(int materiaId, int n, TipoDomanda tipo = TipoDomanda.Nessuna)
    {
        MateriaId = materiaId;
        N = Math.Max(1, n);

        var tutte = await _repo.GetByMateriaIdAsync(MateriaId);
        if (tutte is null || tutte.Count == 0)
        {
            await Shell.Current.DisplayAlert("Errore", "Nessuna domanda disponibile.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Applica filtro per tipologia se richiesto
        List<DomandaQuiz> pool;
        if (tipo == TipoDomanda.Nessuna)
            pool = tutte.ToList();
        else
            pool = tutte.Where(d => d.TipologiaDomanda == tipo).ToList();

        if (pool is null || pool.Count == 0)
        {
            await Shell.Current.DisplayAlert("Errore", "Nessuna domanda disponibile per la tipologia selezionata.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var pick = pool.OrderBy(_ => _rng.Next()).Take(N).ToList();

        // Preparo i nuovi item
        var items = new ObservableCollection<QuizQuestionItem>();
        int numeroDomanda = 1;
        foreach (var d in pick)
        {
            var qItem = new QuizQuestionItem
            {
                QuestionId = d.Id,
                NumeroDomanda = numeroDomanda,
                Testo = d.Domanda,
                CorrectIndex = d.RispostaCorretta,
                SelectedIndex = -1
            };
            numeroDomanda++;
            var opts = d.Risposte
            .Select((t, i) => new QuizOption { Index = i, Text = t, Parent = qItem })
            .ToList();

            qItem.Opzioni = new ObservableCollection<QuizOption>(opts);
            items.Add(qItem);
        }

        // ✨ Assegna su thread UI
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Domande = items;
            System.Diagnostics.Debug.WriteLine($"[QuizViewModel] Caricate {Domande.Count} domande");
        });
    }

    [RelayCommand]
    private void Seleziona(QuizOption? opt)
    {
        if (opt?.Parent is null) return;
        opt.Parent.SelectedIndex = opt.Index;
    }

    [RelayCommand]
    private async Task FineAsync()
    {
        if (Domande.Count == 0) return;

        foreach (var item in Domande)
        {
            if (item.CorrectIndex == item.SelectedIndex)
            {
                await _repo.IncrementaIndovinataAsync(item.QuestionId);
                item.ColoreRisposta = Colors.Green;
            }
            else
            {
                await _repo.IncrementaSbagliataAsync(item.QuestionId);
                item.ColoreRisposta = Colors.Red;
            }

        }
        var corrette = Domande.Count(q => q.SelectedIndex == q.CorrectIndex);
        var sbagliate = Domande.Count - corrette;
        int nrDomandeDaIndovinare = (int)Math.Ceiling((Domande.Count * 18d) / 30d);
        var superato = corrette >= nrDomandeDaIndovinare;

        var msg = $"Risultato: {corrette} corrette, {sbagliate} sbagliate.\n" +
        (superato ? $"✅ Test superato: {corrette}/{Domande.Count}" : $"❌ Test non superato {corrette}/{Domande.Count}. Devi indovinare {nrDomandeDaIndovinare} domande");

        await Shell.Current.DisplayAlert("Esito quiz", msg, "OK");

        ShowAnswers = true;
    }
}
