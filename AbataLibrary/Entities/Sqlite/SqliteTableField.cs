using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbataLibrary.Entities.Sqlite
{
    [AttributeUsage(AttributeTargets.Property, Inherited =false, AllowMultiple = false)]
    class SqliteTableField : Attribute
    {
        private string sqliteTableField;

        public SqliteTableField(string fieldName)
        {
            sqliteTableField = fieldName;
        }

        public virtual string Name
        {
            get { return sqliteTableField; }
        }
    }
}
