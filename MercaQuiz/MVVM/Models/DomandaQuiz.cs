using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MercaQuiz.MVVM.Models;
[Table("DomandeQuiz")]
public class DomandaQuiz
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // FK verso Materia
    [Indexed(Name = "IDX_Domande_Materia")]
    public int MateriaId { get; set; }

    // 👇 Navigazione opzionale (non mappata in DB)
    [Ignore]
    public Materia? Materia { get; set; }



    // Testo della domanda (obbligatorio)
    [NotNull]
    public string Domanda { get; set; } = string.Empty;

    /// <summary>
    /// Numero di volte che si è sbagliata la risposta.
    /// </summary>
    public int SbagliataNrVolte { get; set; } = 0;

    public int IndovinataNrVolte { get; set; } = 0;

    public int TipologiaDomanda { get; set; } = 0; // 0 = domanda quiz, 1 = domanda fine lezione

    /// <summary>
    /// Stringa che indica a quale modulo appartiene la domanda nel caso in cui sia TipologiaDomanda = 1
    /// </summary>
    public string ModuloAppartenenza { get; set; }

    // BACKING FIELD: JSON delle risposte
    [NotNull]
    public string RisposteJson { get; set; } = "[]";

    // Property comoda in app (non mappata in DB)
    [Ignore]
    public List<string> Risposte
    {
        get
        {
            try
            {
                return string.IsNullOrWhiteSpace(RisposteJson)
                    ? new List<string>()
                    : (JsonSerializer.Deserialize<List<string>>(RisposteJson) ?? new List<string>());
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            var list = value ?? new List<string>();
            RisposteJson = JsonSerializer.Serialize(list);
        }
    }

    // Indice della risposta corretta nella lista (0-based)
    [NotNull]
    public int RispostaCorretta { get; set; }

    [Ignore]
    public string RispostaCorrettaTesto
    {
        get
        {
            if(Risposte == null || Risposte.Count == 0)
                return string.Empty;

            var risposte = Risposte;
            if (RispostaCorretta >= 0 && RispostaCorretta < risposte.Count)
                return risposte[RispostaCorretta];
            return string.Empty;
        }
    }

    // Timestamp
    [NotNull]
    public DateTime CreatoIlUtc { get; set; } = DateTime.UtcNow;

    [NotNull]
    public DateTime AggiornatoIlUtc { get; set; } = DateTime.UtcNow;

    // Validazione base
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Domanda))
            throw new ArgumentException("La domanda è obbligatoria.");

        var risposte = Risposte;
        if (risposte.Count < 4)
            throw new ArgumentException("Servono almeno 4 risposte.");
        if (RispostaCorretta < 0 || RispostaCorretta >= risposte.Count)
            throw new ArgumentOutOfRangeException(nameof(RispostaCorretta),
                "L'indice della risposta corretta deve essere compreso tra 0 e Risposte.Count-1.");
        // Normalizza: trim e rimuovi risposte vuote/duplicate adiacenti
        for (int i = 0; i < risposte.Count; i++)
            risposte[i] = risposte[i].Trim();

        // Riallinea JSON se abbiamo trimmato
        RisposteJson = JsonSerializer.Serialize(risposte);
    }
}
