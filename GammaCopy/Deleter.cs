using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace GammaCopy
{
    internal class Deleter : IDisposable
    {
        public Deleter(SQLiteConnection db)
        {
            txn = db.BeginTransaction();
            command = new SQLiteCommand(db)
            {
                CommandText = "DELETE FROM result WHERE id = ?",
                CommandType = CommandType.Text
            };
            param1 = new SQLiteParameter();
            command.Parameters.Add(param1);
        }
        public void Delete(Result res)
        {
            param1.Value = res.Id;
            command.ExecuteNonQuery();
        }
        public DbTransaction txn = null;
        public SQLiteCommand command = null;
        public SQLiteParameter param1 = null;
        public void Dispose()
        {
            command.Dispose();
            txn.Commit();
            txn.Dispose();
        }
    }
}
