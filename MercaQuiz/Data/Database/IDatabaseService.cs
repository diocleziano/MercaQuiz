using SQLite;

namespace MercaQuiz.Data.Database;
public interface IDatabaseService
{
    SQLiteAsyncConnection Connection { get; }
    Task InitializeAsync(); // crea tabelle, indici, seed, ecc.
}
