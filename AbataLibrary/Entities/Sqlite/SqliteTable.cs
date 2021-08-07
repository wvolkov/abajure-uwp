using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbataLibrary.Entities.Sqlite
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    class SqliteTable : Attribute
    {
        private string sqliteTableName;

        public SqliteTable(string tableName)
        {
            sqliteTableName = tableName;
        }

        public virtual string Name
        {
            get { return sqliteTableName; }
        }
    }
}
