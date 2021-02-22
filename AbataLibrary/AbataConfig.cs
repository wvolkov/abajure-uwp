using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AbataLibrary
{
    public static class AbataConfig
    {
        /// <summary>
        /// SQLite Data Base file path
        /// </summary>
        public static string DB_PATH { get; }

        static AbataConfig()
        {
            DB_PATH = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Abadata.edb");
        }
    }
}
