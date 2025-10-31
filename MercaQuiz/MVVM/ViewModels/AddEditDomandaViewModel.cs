using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MercaQuiz.Data.Repository;
using MercaQuiz.Global;
using MercaQuiz.Helpers;
using MercaQuiz.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

    // Nuovi campi per tipologia e modulo
    public List<string> TipoItems { get; } = new() { "Domanda Quiz", "Domanda Fine Lezione" };

    [ObservableProperty]
    private int selectedTipoIndex = 0; // 0 -> DomandaQuiz, 1 -> DomandaFineLezione

    partial void OnSelectedTipoIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFineLezione));
        // Rerun parse quando cambio la tipologia (solo se ho testo grezzo incollato)
        if (!string.IsNullOrWhiteSpace(RawText))
        {
            Parse();
        }
    }

    public TipoDomanda SelectedTipo => (TipoDomanda)Math.Clamp(SelectedTipoIndex + 1, 1, 2);

    public bool IsFineLezione => SelectedTipo == TipoDomanda.DomandaFineLezione;

    [ObservableProperty]
    private string moduloAppartenenza = string.Empty;

    partial void OnRawTextChanged(string? value) => Parse();

    [RelayCommand]
    private void Parse()
    {
        var (q, answers) = ParseQA(RawText ?? string.Empty, SelectedTipo);

        Question = q ?? string.Empty;

        // Porta i risultati anche nei 4 campi “nuovi”
        Answer1 = answers.ElementAtOrDefault(0) ?? string.Empty;
        Answer2 = answers.ElementAtOrDefault(1) ?? string.Empty;
        Answer3 = answers.ElementAtOrDefault(2) ?? string.Empty;
        Answer4 = answers.ElementAtOrDefault(3) ?? string.Empty;

        // Mantieni coerente anche la CollectionView “vecchia”
        SyncAnswersToCollection(new[]
        {
        Answer1 ?? string.Empty,
        Answer2 ?? string.Empty,
        Answer3 ?? string.Empty,
        Answer4 ?? string.Empty
    });
    }

    private void SyncAnswersToCollection(IEnumerable<string> answers)
    {
        var arr = answers.ToArray();
        EnsureAtLeastFourAnswers(); // garantisce 4 elementi nella collection

        for (int i = 0; i < 4; i++)
        {
            var value = arr.ElementAtOrDefault(i) ?? string.Empty;
            if (i < Risposte.Count)
            {
                Risposte[i].Text = value;
            }
            else
            {
                Risposte.Add(new AnswerItem { Index = i, Text = value });
            }
        }
        for (int i = 0; i < Risposte.Count; i++) Risposte[i].Index = i;
    }


    private static (string Question, string[] Answers) ParseQA(string input, TipoDomanda tipo)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, Array.Empty<string>());

        // Normalizzo e tolgo solo righe vuote (così gli indici restano stabili)
        var lines = input
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
            return (string.Empty, Array.Empty<string>());

        // Domanda = prima riga (senza rimuovere nulla)
        var question = lines[0];

        if (tipo == TipoDomanda.DomandaQuiz)
        {
            // Comportamento classico: numerate tipo 1., 2), ecc.
            var rawAnswers = lines
                .Skip(1)
                .Where(IsLikelyAnswerLine)
                .Take(4)
                .Select(s => StripLeadingNumberToken(s).Trim())
                .ToList();

            if (rawAnswers.Count < 4)
            {
                rawAnswers = lines
                    .Skip(1)
                    .Select(s => StripLeadingNumberToken(s).Trim())
                    .Where(s => s.Length > 0)
                    .Take(4)
                    .ToList();
            }

            while (rawAnswers.Count < 4) rawAnswers.Add(string.Empty);
            return (question, rawAnswers.Take(4).ToArray());
        }
        else // TipoDomanda.DomandaFineLezione
        {
            // ======= TENTATIVO POSIZIONALE =======
            // Atteso: [0]=Q, [1]=Paragrafo..., [2]=A, [3]=AnsA, [4]=B, [5]=AnsB, [6]=C, [7]=AnsC, [8]=D, [9]=AnsD
            bool looksPositional =
                   lines.Count >= 10
                && Regex.IsMatch(lines[2], @"^[A]$", RegexOptions.IgnoreCase)
                && Regex.IsMatch(lines[4], @"^[B]$", RegexOptions.IgnoreCase)
                && Regex.IsMatch(lines[6], @"^[C]$", RegexOptions.IgnoreCase)
                && Regex.IsMatch(lines[8], @"^[D]$", RegexOptions.IgnoreCase);

            if (looksPositional)
            {
                var ans = new[]
                {
                lines[3].Trim(),
                lines[5].Trim(),
                lines[7].Trim(),
                lines[9].Trim()
            };
                return (question, ans);
            }

            // ======= FALLBACK A ETICHETTE A/B/C/D =======
            var labelRx = new Regex(@"^\s*\(?([A-D])\)?[\.\):\-–]?\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var answers = new string[4]; // A=0, B=1, C=2, D=3
            var found = new bool[4];

            for (int i = 1; i < lines.Count; i++)
            {
                var m = labelRx.Match(lines[i]);
                if (!m.Success) continue;

                int idx = char.ToUpperInvariant(m.Groups[1].Value[0]) - 'A';
                if (idx < 0 || idx > 3 || found[idx]) continue;

                var inline = m.Groups[2].Value?.Trim();
                if (!string.IsNullOrEmpty(inline))
                {
                    answers[idx] = inline;
                    found[idx] = true;
                }
                else
                {
                    // testo sulla riga successiva “sostanziosa”
                    var next = lines.Skip(i + 1)
                                    .FirstOrDefault(s => !labelRx.IsMatch(s) && !string.IsNullOrWhiteSpace(s));
                    answers[idx] = next?.Trim() ?? string.Empty;
                    found[idx] = true;
                }
            }

            // Se qualche slot manca, prova a completare con le prime righe utili dopo la domanda
            if (found.Count(b => b) < 4)
            {
                var pool = lines.Skip(1)
                                .Where(s => !labelRx.IsMatch(s))
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Select(s => s.Trim())
                                .ToList();

                int p = 0;
                for (int k = 0; k < 4; k++)
                {
                    if (!found[k])
                    {
                        answers[k] = p < pool.Count ? pool[p++] : string.Empty;
                    }
                }
            }

            return (question, answers);
        }
    }

    private static bool IsLikelyAnswerLine(string line)
    {
        return Regex.IsMatch(line, @"^\s*\(?\d{1,2}\)?\s*[\.\):\-–:]?\s+.+$");
    }
    private static string StripLeadingNumberToken(string line)
    {
        return Regex.Replace(line, @"^\s*\(?\d{1,2}\)?\s*[\.\):\-–:]?\s*", "").Trim();
    }

    public AddEditDomandaViewModel(DomandeQuizRepository repo)
    {
        _repo = repo;
        IsEditMode = false;
        // default: 4 righe vuote
        EnsureAtLeastFourAnswers();
        //ModuloAppartenenza = AddDomandaGlobalData.ModuloDomanda ?? string.Empty;
    }

    public void InitForMateria(int materiaId)
    {
        MateriaId = materiaId;
        EnsureAtLeastFourAnswers();
    }

    private void EnsureAtLeastFourAnswers()
    {
        if (Risposte is null)
            Risposte = new ObservableCollection<AnswerItem>();

        var start = Risposte.Count;
        for (int i = start; i < 4; i++)
            Risposte.Add(new AnswerItem { Index = i, Text = string.Empty });

        for (int i = 0; i < Risposte.Count; i++) Risposte[i].Index = i;

        if (RispostaCorrettaIndex < 0 || RispostaCorrettaIndex >= 4)
            RispostaCorrettaIndex = 0;
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
        // ✅ allinea anche la collection "vecchia"
        SyncAnswersToCollection(new[]
        {
            Answer1 ?? string.Empty,
            Answer2 ?? string.Empty,
            Answer3 ?? string.Empty,
            Answer4 ?? string.Empty
        });
        // Nuovi campi: tipologia e modulo
        SelectedTipoIndex = Math.Clamp(((int)d.TipologiaDomanda) - 1, 0, TipoItems.Count - 1);
        ModuloAppartenenza = d.ModuloAppartenenza ?? string.Empty;
        AddDomandaGlobalData.ModuloDomanda = ModuloAppartenenza;
    }

    [RelayCommand]
    private void ImpostaModuloLezione()
    {
        if(!string.IsNullOrWhiteSpace(AddDomandaGlobalData.ModuloDomanda))
            ModuloAppartenenza = AddDomandaGlobalData.ModuloDomanda;
    }

    [RelayCommand]
    private void SegnaCorrettaIndex(int index)
    {
        if (index < 0 || index > 3) return;
        RispostaCorrettaIndex = index;
    }

    [RelayCommand]
    private async Task SalvaAsync()
    {
        // ✅ 1) Sincronizza i NUOVI campi → vecchi
        // Se stai usando la nuova UI, Question/AnswerX saranno valorizzati: usali.
        var domandaRaw = string.IsNullOrWhiteSpace(Question) ? (TestoDomanda ?? string.Empty) : Question!;
        // Preferisci sempre i 4 campi nuovi, trim e filtra vuoti
        var answers = new[]
        {
            Answer1?.Trim(), Answer2?.Trim(), Answer3?.Trim(), Answer4?.Trim()
        }
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s!)
        .ToList();

        // Se mancano, prova dalla collection "vecchia"
        if (answers.Count < 4 && (Risposte?.Any() == true))
        {
            answers = Risposte
                .Select(r => (r.Text ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(4)
                .ToList();

            // e riallinea i campi nuovi se servisse
            Answer1 = answers.ElementAtOrDefault(0) ?? string.Empty;
            Answer2 = answers.ElementAtOrDefault(1) ?? string.Empty;
            Answer3 = answers.ElementAtOrDefault(2) ?? string.Empty;
            Answer4 = answers.ElementAtOrDefault(3) ?? string.Empty;
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
                AggiornatoIlUtc = now,
                TipologiaDomanda = SelectedTipo,
                ModuloAppartenenza = ModuloAppartenenza?.Trim() ?? string.Empty
            };

            try
            {
                entity.Validate();
                AddDomandaGlobalData.ModuloDomanda = entity.ModuloAppartenenza;
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

            // Nuovi campi: tipologia e modulo
            entity.TipologiaDomanda = SelectedTipo;
            entity.ModuloAppartenenza = ModuloAppartenenza?.Trim() ?? string.Empty;

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