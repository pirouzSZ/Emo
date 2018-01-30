using SQLite;

namespace Emo
{
    public interface ISQLiteDb
    {
        SQLiteAsyncConnection GetConnection();
    }
}
