using System.Collections.Generic;
using Truffle.Database;

namespace Truffle.Model
{
    public class GenericSqlObject: PartialSqlObject
    {
        public string Id {get;set;}
        public string Table {get;set;}

        public GenericSqlObject() {}
        public GenericSqlObject(string table, string id)
        {
            this.Id = id;
            this.Table = table;
        }

        public GenericSqlObject(string table, string id, object value, DatabaseConnector database)
        {
            this.Id = id;
            this.Table = table;
            base.initFromDatabase(value, id, database);
        }

        public GenericSqlObject(string table, string id, object value, string column, DatabaseConnector database)
        {
            this.Id = id;
            this.Table = table;
            base.initFromDatabase(value, column, database);
        }

        public GenericSqlObject(string table, string id, Dictionary<string, object> values)
        {
            this.Id = id;
            this.Table = table;
            LoadValues(values);
        }

        public override string GetId()
        {
            return Id;
        }

        public override string GetTable()
        {
            return Table;
        }
    }
}