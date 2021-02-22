using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace AbataLibrary
{
    public class AbataProvider
    {
        public bool DBExists { get; private set; }

        public AbataProvider()
        {
            InitializeProvider();
        }

        private async void InitializeProvider()
        {
            try
            {
                StorageFile db = await StorageFile.GetFileFromPathAsync(AbataConfig.DB_PATH);
                DBExists = true;
            }
            catch
            {
                DBExists = false;
            }
        }

        public void CreateDataBase()
        {
            using (SqliteConnection db = new SqliteConnection(String.Format("Filename={0}", AbataConfig.DB_PATH)))
            {
                db.Open();
                CreateBaseTables(db);
                db.Close();
            }
        }



        private void CreateBaseTables(SqliteConnection db)
        {
            string sqlCmd;
            //Song Table

            sqlCmd = "CREATE TABLE IF NOT " +
                        "EXISTIS Songs (" +
                                        "SongId INTEGER PRIMARY KEY AUTOINCREMENT," +
                                        "";
        }
    }
}

