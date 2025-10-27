using SQLite;

namespace MercaQuiz.MVVM.Models;

[Table("Materie")]
public class Materia
{
    // Chiave primaria autoincrementale
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Nome della materia (obbligatorio)
    [NotNull, Indexed(Name = "IDX_Materia_Nome")]
    public string Nome { get; set; } = string.Empty;

    // Codice materia (univoco, opzionale ma consigliato)
    [Unique, Indexed(Name = "IDX_Materia_Codice")]
    public string? Codice { get; set; }

    /// <summary>
    /// Facoltà di appartenenza (es. Ingegneria, Economia)
    /// </summary>
    public string Facolta { get; set; }

    /// <summary>
    /// Anno accademico di appartenenza (1, 2, 3)
    /// </summary>
    public int AnnoAccademico { get; set; }

    // Docente principale
    [Indexed(Name = "IDX_Materia_Docente")]
    public string? Docente { get; set; }

    // Crediti/CFU o ore totali
    public int? Crediti { get; set; }

    // Colore in formato HEX (#RRGGBB) per UI
    [MaxLength(7)]
    public string? ColoreHex { get; set; }

    // Icona (nome risorsa o emoji)
    public string? Icona { get; set; }

    /// <summary>
    /// Data prossimo esame
    /// </summary>
    public DateTime? DataProssimoEsame { get; set; }

    public bool IsSuperato { get; set; }

    public int NumeroTentativi { get; set; }
    public int VotoEsame { get; set; }
    public int NumeroQuizEffettuati { get; set; }

    public int NumeroQuizSuperati { get; set; }
    public int NumeroQuizFalliti { get; set; }

    // Note libere
    public string? Note { get; set; }

    // Flag per archiviazione (soft-delete)
    [Indexed(Name = "IDX_Materia_Archivio")]
    public bool IsArchiviata { get; set; }

    // Timestamp creazione/aggiornamento (UTC)
    [NotNull]
    public DateTime CreatoIlUtc { get; set; } = DateTime.UtcNow;

    [NotNull]
    public DateTime AggiornatoIlUtc { get; set; } = DateTime.UtcNow;

    // 👇 Relazione 1:N con DomandeQuiz (non mappata direttamente)
    [Ignore]
    public List<DomandaQuiz> Domande { get; set; } = new();
}
