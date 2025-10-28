using MercaQuiz.Config;
using MercaQuiz.MVVM.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MercaQuiz.Data.Database;

public sealed class DatabaseService : IDatabaseService
{
    private readonly Lazy<SQLiteAsyncConnection> _lazyConn;

    public SQLiteAsyncConnection Connection => _lazyConn.Value;

    public DatabaseService()
    {
        _lazyConn = new Lazy<SQLiteAsyncConnection>(() =>
        {
            var dbPath = AppConfigService.GetDatabaseFullPath();

            // Assicura che la cartella esista
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
            return new SQLiteAsyncConnection(dbPath, flags);
        });
    }

    public async Task InitializeAsync()
    {
        // Crea (o migra) le tabelle se non esistono
        await Connection.CreateTableAsync<Materia>();
        await Connection.CreateTableAsync<DomandaQuiz>();
    }
}
