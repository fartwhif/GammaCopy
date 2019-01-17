using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace GammaCopy
{
    internal class Inserter : IDisposable
    {
        public Inserter(SQLiteConnection db)
        {
            txn = db.BeginTransaction();
            lastInsertIdCmd = db.CreateCommand();
            lastInsertIdCmd.CommandText = @"SELECT last_insert_rowid()";
            command = new SQLiteCommand(db)
            {
                CommandText = "INSERT INTO result (parent,path,md5,length,lastwrite,created,archive_index,md5_0,md5_1,pathmd5_0,pathmd5_1) values (?,?,?,?,?,?,?,?,?,?,?)",
                CommandType = CommandType.Text
            };
            param1 = new SQLiteParameter();
            param2 = new SQLiteParameter();
            param3 = new SQLiteParameter();
            param4 = new SQLiteParameter();
            param5 = new SQLiteParameter();
            param6 = new SQLiteParameter();
            param7 = new SQLiteParameter();
            param8 = new SQLiteParameter();
            param9 = new SQLiteParameter();
            param10 = new SQLiteParameter();
            param11 = new SQLiteParameter();

            command.Parameters.Add(param1);
            command.Parameters.Add(param2);
            command.Parameters.Add(param3);
            command.Parameters.Add(param4);
            command.Parameters.Add(param5);
            command.Parameters.Add(param6);
            command.Parameters.Add(param7);
            command.Parameters.Add(param8);
            command.Parameters.Add(param9);
            command.Parameters.Add(param10);
            command.Parameters.Add(param11);

        }
        public void Insert(Result res)
        {
            param1.Value = res.ParentId;
            param2.Value = res.Path;
            param3.Value = res.Md5;
            param4.Value = res.Length;
            param5.Value = (int)res.Modified.ToTimestamp();
            param6.Value = (int)res.Created.ToTimestamp();
            param7.Value = res.ArchiveIndex;
            param8.Value = res.Md5Split.Item1;
            param9.Value = res.Md5Split.Item2;
            param10.Value = res.PathMd5Split.Item1;
            param11.Value = res.PathMd5Split.Item2;

            command.ExecuteNonQuery();
            lastInsertIdCmd.ExecuteNonQuery();
            res.Id = Convert.ToInt64(lastInsertIdCmd.ExecuteScalar());
        }
        public DbTransaction txn = null;
        public SQLiteCommand command = null;
        public SQLiteCommand lastInsertIdCmd = null;
        public SQLiteParameter param1 = null;
        public SQLiteParameter param2 = null;
        public SQLiteParameter param3 = null;
        public SQLiteParameter param4 = null;
        public SQLiteParameter param5 = null;
        public SQLiteParameter param6 = null;
        public SQLiteParameter param7 = null;
        public SQLiteParameter param8 = null;
        public SQLiteParameter param9 = null;
        public SQLiteParameter param10 = null;
        public SQLiteParameter param11 = null;
        public void Dispose()
        {
            command.Dispose();
            lastInsertIdCmd.Dispose();
            txn.Commit();
            txn.Dispose();
        }
    }
}
