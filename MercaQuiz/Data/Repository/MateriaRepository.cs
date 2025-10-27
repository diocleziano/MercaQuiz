using MercaQuiz.Data.Database;
using MercaQuiz.MVVM.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MercaQuiz.Data.Repository;
public class MaterieRepository
{
    private readonly SQLiteAsyncConnection _db;
    public MaterieRepository(IDatabaseService dbService) => _db = dbService.Connection;

    public async Task InitAsync()
    {
        await _db.CreateTableAsync<Materia>();
        // Indici aggiuntivi compositi (se ti servono):
        // await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS IDX_Materia_Attiva ON Materie(IsArchiviata)");
    }

    public Task<int> InsertAsync(Materia m)
    {
        m.CreatoIlUtc = DateTime.UtcNow;
        m.AggiornatoIlUtc = m.CreatoIlUtc;
        return _db.InsertAsync(m);
    }

    public Task<int> UpdateAsync(Materia m)
    {
        m.AggiornatoIlUtc = DateTime.UtcNow;
        return _db.UpdateAsync(m);
    }

    public Task<int> DeleteAsync(Materia m) => _db.DeleteAsync(m);

    public Task<List<Materia>> GetAllAsync(bool includiArchiviate = false)
    {
        if (includiArchiviate)
            return _db.Table<Materia>().OrderBy(x => x.Nome).ToListAsync();

        return _db.Table<Materia>()
                  .Where(x => !x.IsArchiviata)
                  .OrderBy(x => x.Nome)
                  .ToListAsync();
    }

    public Task<Materia?> GetByIdAsync(int id) =>
        _db.FindAsync<Materia>(id);

    public Task<int> CountByMateriaAsync(int materiaId) =>
    _db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM DomandeQuiz WHERE MateriaId = ?", materiaId);

    public Task<Materia?> GetByCodiceAsync(string codice) =>
        _db.Table<Materia>().Where(x => x.Codice == codice).FirstOrDefaultAsync();

    public Task<int> ArchiviaAsync(int id, bool archivia = true) =>
        _db.ExecuteAsync("UPDATE Materie SET IsArchiviata=?, AggiornatoIlUtc=? WHERE Id=?",
                         archivia, DateTime.UtcNow, id);
}
