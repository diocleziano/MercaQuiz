using MercaQuiz.Data.Database;
using MercaQuiz.MVVM.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MercaQuiz.Data.Repository;
public class DomandeQuizRepository
{
    private readonly SQLiteAsyncConnection _db;

    public DomandeQuizRepository(IDatabaseService dbService) => _db = dbService.Connection;

    public async Task InitAsync()
    {
        await _db.CreateTableAsync<DomandaQuiz>();
        // Evita duplicati (Domanda) per stessa materia (facoltativo, rimuovi se non ti serve)
        await _db.ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS UX_Domande_Materia_Domanda ON DomandeQuiz(MateriaId, Domanda)");
    }

    public async Task<int> InsertAsync(DomandaQuiz d)
    {
        d.Validate();
        d.CreatoIlUtc = d.AggiornatoIlUtc = DateTime.UtcNow;
        var domande = await GetByMateriaIdAsync(d.MateriaId); // carica le domande per la materia (se necessario)
        var domandeForseUguali = domande.Where(x => x.Domanda.ToLowerInvariant() == d.Domanda.ToLowerInvariant()).ToList();
        if (domandeForseUguali.Count > 0)
        {

            bool risposteUgualiPresenti = false;
            List<string> risposteLowerCase = d.Risposte.Select(x => x.ToLowerInvariant()).ToList();
            foreach (var item in domandeForseUguali)
            {
                foreach (var opzione in item.Risposte)
                {
                    if (risposteLowerCase.Contains(opzione.ToLowerInvariant()))
                        risposteUgualiPresenti = true;
                    else
                    {
                        risposteUgualiPresenti = false;
                        break;
                    }
                }
            }

            if (risposteUgualiPresenti)
            {
                throw new InvalidOperationException("Esiste già una domanda identica per questa materia.");
            }
        }
        return await _db.InsertAsync(d);
    }

    public async Task<int> UpdateAsync(DomandaQuiz d)
    {
        d.Validate();
        d.AggiornatoIlUtc = DateTime.UtcNow;
        return await _db.UpdateAsync(d);
    }

    public Task<int> DeleteAsync(DomandaQuiz d) => _db.DeleteAsync(d);

    public Task<int> DeleteByMateriaAsync(int materiaId) =>
        _db.ExecuteAsync("DELETE FROM DomandeQuiz WHERE MateriaId = ?", materiaId);

    public Task<List<DomandaQuiz>> GetByMateriaIdAsync(int materiaId) =>
        _db.Table<DomandaQuiz>()
           .Where(x => x.MateriaId == materiaId)
           .OrderBy(x => x.Id)
           .ToListAsync();

    public Task<DomandaQuiz?> GetByIdAsync(int id) =>
        _db.Table<DomandaQuiz>().Where(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<bool> ExistsByMateriaAndTextAsync(int materiaId, string domandaNormalized, int excludeId = 0, CancellationToken ct = default)
    {
        // DomandaNormalized è una colonna calcolata/valorizzata lato app (come nel VM)
        const string sql = @"
        SELECT 1
        FROM DomandeQuiz
        WHERE MateriaId = @MateriaId
          AND DomandaNormalized = @DomandaNormalized
          AND (@ExcludeId = 0 OR Id <> @ExcludeId)
        LIMIT 1;";

        // usa il tuo wrapper/ORM
        return await _db.ExecuteScalarAsync<int>(
            sql,
            new { MateriaId = materiaId, DomandaNormalized = domandaNormalized, ExcludeId = excludeId },
            ct) == 1;
    }


    public Task<int> IncrementaSbagliataAsync(int id)
    {
        // _conn è SQLiteAsyncConnection
        // NB: usa parametri per sicurezza
        var now = DateTime.UtcNow;
        return _db.ExecuteAsync(
            "UPDATE DomandeQuiz " +
            "SET SbagliataNrVolte = SbagliataNrVolte + 1, AggiornatoIlUtc = ? " +
            "WHERE Id = ?",
            now, id);
    }

    public Task<int> IncrementaIndovinataAsync(int id)
    {
        // _conn è SQLiteAsyncConnection
        // NB: usa parametri per sicurezza
        var now = DateTime.UtcNow;
        return _db.ExecuteAsync(
            "UPDATE DomandeQuiz " +
            "SET IndovinataNrVolte = IndovinataNrVolte + 1, AggiornatoIlUtc = ? " +
            "WHERE Id = ?",
            now, id);
    }

    public Task<int> ResetConteggiAsync()
    {
        // _conn è SQLiteAsyncConnection
        // NB: usa parametri per sicurezza
        var now = DateTime.UtcNow;
        return _db.ExecuteAsync(
            "UPDATE DomandeQuiz " +
            "SET IndovinataNrVolte = 0, SbagliataNrVolte = 0, AggiornatoIlUtc = ? ",
            now);
    }

    public Task<int> CountByMateriaAsync(int materiaId) =>
        _db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM DomandeQuiz WHERE MateriaId = ?", materiaId);
}
