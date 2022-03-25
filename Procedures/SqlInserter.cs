using System;
using System.Collections.Generic;
using Truffle.Database;
using Truffle.Model;

namespace Truffle.Procedures
{
    public class SqlInserter : SqlEditor
    {
        public SqlInserter() : base() {}

        public SqlInserter(SqlObject o) : base(o) {}

        public Boolean Insert(string table, DatabaseConnector database)
        {
            Dictionary<string, string> fields = GetFields();
            if (fields.Count == 0) return true;

            string command = $"insert {table} ({String.Join(',', fields.Keys)}) values ({String.Join(',',fields.Values)})";
            try {
                database.RunCommand(command);
                return true;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }
    }
}