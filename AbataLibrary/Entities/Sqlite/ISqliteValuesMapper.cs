using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbataLibrary.Entities.Sqlite
{
    interface ISqliteValuesMapper
    {
        Dictionary<string, object> MapValuesToDb();
    }
}
