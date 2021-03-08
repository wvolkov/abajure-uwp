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
        private static AbataProvider _aba;

        public static AbataProvider GetProvider()
        {
            if (_aba == null)
                _aba = new AbataProvider();
            return _aba;
        }
        public bool DBExists { get; private set; }

        private AbataProvider()
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
                CreateBaseTables(db);
        }



        private void CreateBaseTables(SqliteConnection db)
        {
            string sqlCmd = "";
            db.Open();

            SqliteCommand command = db.CreateCommand();
            //Songs Table
            sqlCmd = @"CREATE TABLE `songs` (
	                                `id`	INTEGER PRIMARY KEY AUTOINCREMENT,
	                                `song_path`	TEXT,
	                                `song_hash`	BLOB,
	                                `title`	TEXT,
	                                `artist`	TEXT,
	                                `album`	TEXT,
	                                `album_artist`	TEXT,
	                                `subtitle`	TEXT,
	                                `bitrate`	INTEGER,
	                                `date_modifed`	REAL,
	                                `duration`	REAL
                                );";

            command.CommandText = sqlCmd;

            command.ExecuteNonQuery();

            db.Close();
        }
    }
}

