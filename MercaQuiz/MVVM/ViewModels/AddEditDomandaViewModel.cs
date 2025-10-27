using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MercaQuiz.Data.Repository;
using MercaQuiz.Helpers;
using MercaQuiz.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MercaQuiz.MVVM.ViewModels;

public class AnswerItem : ObservableObject
{
    public int Index { get; set; }

    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}

public partial class AddEditDomandaViewModel : ObservableObject
{
    private readonly DomandeQuizRepository _repo;
    private IMessenger _messenger = WeakReferenceMessenger.Default;
    public int MateriaId { get; private set; }

    [ObservableProperty] private string testoDomanda = string.Empty;
    [ObservableProperty] private ObservableCollection<AnswerItem> risposte = new();

    [ObservableProperty] private int rispostaCorrettaIndex = 0; // 0-based
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private int domandaId; // per eventuale modifica futura

    [ObservableProperty] private string? rawText;
    [ObservableProperty] private string? question;
    [ObservableProperty] private string? answer1;
    [ObservableProperty] private string? answer2;
    [ObservableProperty] private string? answer3;
    [ObservableProperty] private string? answer4;

    partial void OnRawTextChanged(string? value) => Parse();

    [RelayCommand]
    private void Parse()
    {
        var (q, answers) = ParseQA(RawText ?? string.Empty);

        Question = q ?? string.Empty;
        Answer1 = answers.ElementAtOrDefault(0) ?? string.Empty;
        Answer2 = answers.ElementAtOrDefault(1) ?? string.Empty;
        Answer3 = answers.ElementAtOrDefault(2) ?? string.Empty;
        Answer4 = answers.ElementAtOrDefault(3) ?? string.Empty;
    }

    /// <summary>
    /// Estrae domanda e 4 risposte dal testo incollato.
    /// Rimuove prefissi numerici come "1", "1)", "1.", "1 -", "1:", "1\t", "(1)" ecc.
    /// </summary>
    private static (string Question, string[] Answers) ParseQA(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, Array.Empty<string>());

        // Normalizza e split
        var lines = input
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
            return (string.Empty, Array.Empty<string>());

        // Prima riga = domanda
        var question = lines[0];

        // Righe successive: cerca quelle "answer-like"
        var rawAnswers = lines
            .Skip(1)
            .Where(IsLikelyAnswerLine)
            .Take(4)
            .Select(StripLeadingNumberToken)
            .Select(s => s.Trim())
            .ToList();

        // Fallback: se non bastano, prendi le successive righe non vuote
        if (rawAnswers.Count < 4)
        {
            var fallback = lines
                .Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(StripLeadingNumberToken)
                .Select(s => s.Trim())
                .Take(4)
                .ToList();

            rawAnswers = fallback;
        }

        // Padding a 4
        while (rawAnswers.Count < 4) rawAnswers.Add(string.Empty);

        return (question, rawAnswers.Take(4).ToArray());
    }

    private static bool IsLikelyAnswerLine(string line)
    {
        // Pattern tipici: "1", "1)", "1.", "1 -", "1:", "1\t", "(1)" + testo
        return Regex.IsMatch(line, @"^\s*\(?\d{1,2}\)?\s*[\.\):\-–:]?\s+.+$")
               || Regex.IsMatch(line, @"^\s*\d{1,2}\s+.+$")
               || Regex.IsMatch(line, @"^\s*\d{1,2}\t+.+$");
    }

    private static string StripLeadingNumberToken(string line)
    {
        // Rimuove prefissi numerici iniziali con vari separatori
        return Regex.Replace(line, @"^\s*\(?\d{1,2}\)?\s*[\.\):\-–:]?\s*", "").Trim();
    }

    public AddEditDomandaViewModel(DomandeQuizRepository repo)
    {
        _repo = repo;
        IsEditMode = false;
        // default: 4 righe vuote
        EnsureAtLeastFourAnswers();
    }

    public void InitForMateria(int materiaId)
    {
        MateriaId = materiaId;
        EnsureAtLeastFourAnswers();
    }

    private void EnsureAtLeastFourAnswers()
    {
        if (Risposte.Count < 4)
        {
            var start = Risposte.Count;
            for (int i = start; i < 4; i++)
                Risposte.Add(new AnswerItem { Index = i, Text = string.Empty });
        }
        for (int i = 0; i < Risposte.Count; i++) Risposte[i].Index = i;
        if (RispostaCorrettaIndex < 0 || RispostaCorrettaIndex >= Risposte.Count) RispostaCorrettaIndex = 0;
    }


    public async Task LoadForEditAsync(int materiaId, int domandaId)
    {
        MateriaId = materiaId;
        DomandaId = domandaId;
        IsEditMode = true;

        var d = await _repo.GetByIdAsync(domandaId);
        if (d is null)
        {
            await Shell.Current.DisplayAlert("Errore", "Domanda non trovata.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Vecchi campi (compatibilità con altre UI che già li usano)
        TestoDomanda = d.Domanda;
        Risposte = new ObservableCollection<AnswerItem>(
            d.Risposte.Select((txt, i) => new AnswerItem { Index = i, Text = txt })
        );
        EnsureAtLeastFourAnswers();
        RispostaCorrettaIndex = d.RispostaCorretta;

        // ✅ Popola anche i NUOVI campi per la tua nuova UI
        Question = d.Domanda;
        Answer1 = d.Risposte.ElementAtOrDefault(0) ?? string.Empty;
        Answer2 = d.Risposte.ElementAtOrDefault(1) ?? string.Empty;
        Answer3 = d.Risposte.ElementAtOrDefault(2) ?? string.Empty;
        Answer4 = d.Risposte.ElementAtOrDefault(3) ?? string.Empty;
    }

   

    [RelayCommand]
    private void SegnaCorrettaIndex(int index)
    {
        if (index < 0) return;
        RispostaCorrettaIndex = index;
    }

    [RelayCommand]
    private async Task SalvaAsync()
    {
        // ✅ 1) Sincronizza i NUOVI campi → vecchi
        // Se stai usando la nuova UI, Question/AnswerX saranno valorizzati: usali.
        var domandaRaw = string.IsNullOrWhiteSpace(Question) ? (TestoDomanda ?? string.Empty) : Question!;
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

        // Se per caso la UI “vecchia” (CollectionView) è usata, prova ad attingere da lì
        if (answers.Count < 4 && Risposte?.Any() == true)
        {
            var fromCollection = Risposte.Select(r => (r.Text ?? string.Empty).Trim())
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .ToList();
            answers = fromCollection;
        }

        // ✅ 2) Validazioni minime
        if (string.IsNullOrWhiteSpace(domandaRaw))
        {
            await Shell.Current.DisplayAlert("Errore", "Il testo della domanda è obbligatorio.", "OK");
            return;
        }
        if (answers.Count < 4)
        {
            await Shell.Current.DisplayAlert("Errore", "Inserisci almeno 4 risposte non vuote.", "OK");
            return;
        }
        if (RispostaCorrettaIndex < 0 || RispostaCorrettaIndex >= answers.Count)
        {
            // Se la corretta è fuori range, rimettila a 0
            RispostaCorrettaIndex = 0;
        }

        // ✅ 3) Mappa sull’entità
        var now = DateTime.UtcNow;
        DomandaQuiz entity;

        if (!IsEditMode) // INSERT
        {
            entity = new DomandaQuiz
            {
                MateriaId = MateriaId,
                Domanda = domandaRaw.Trim(),
                Risposte = answers, // setter popola RisposteJson
                RispostaCorretta = RispostaCorrettaIndex,
                CreatoIlUtc = now,
                AggiornatoIlUtc = now
            };

            try
            {
                entity.Validate();
                await _repo.InsertAsync(entity); // firma reale senza ct
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Errore salvataggio", ex.Message, "OK");
#endif
                return;
            }
        }
        else // UPDATE
        {
            entity = await _repo.GetByIdAsync(DomandaId);
            if (entity is null)
            {
                await Shell.Current.DisplayAlert("Errore", "Domanda non trovata.", "OK");
                return;
            }

            entity.Domanda = domandaRaw.Trim();
            entity.Risposte = answers;
            entity.RispostaCorretta = RispostaCorrettaIndex;
            entity.AggiornatoIlUtc = now;

            try
            {
                entity.Validate();
                await _repo.UpdateAsync(entity); // firma reale senza ct
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Errore salvataggio", ex.Message, "OK");
                return;
            }
        }

        // Notifica e chiudi
        _messenger.Send(new DomandaChangedMessage(MateriaId));
        await Shell.Current.GoToAsync("..");
    }
    // Helpers
    private static string NormalizeText(string s)
    {
        // lower-case, collapse spazi, rimuovi spazi ai bordi
        var t = s.ToLowerInvariant().Trim();
        t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ");
        return t;
    }

    [RelayCommand]
    private async Task AnnullaAsync() => await Shell.Current.GoToAsync("..");

    
}