using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AbataLibrary.Entities;
using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace AbataLibrary.Controllers
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
            {
                string sqlCmd = "";
                db.Open();

                SqliteCommand command = db.CreateCommand();
                //Songs Table
                sqlCmd = @"CREATE TABLE `songs` (
	                                `id`	        INTEGER PRIMARY KEY AUTOINCREMENT,
	                                `song_path`	    TEXT,
	                                `song_hash`	    TEXT,
	                                `title`	        TEXT,
	                                `artist`	    TEXT,
	                                `album`	        TEXT,
	                                `album_artist`	TEXT,
	                                `subtitle`	    TEXT,
                                    `track_number`  INTEGER,
                                    `year`          INTEGER,
	                                `bitrate`	    INTEGER,
	                                `date_modified`	TEXT,
	                                `duration`	    TEXT
                                );";

                command.CommandText = sqlCmd;

                command.ExecuteNonQuery();

                db.Close();
            }
        }

        public void InsertSongs(SongSet songs)
        {
            using (SqliteConnection db = new SqliteConnection(String.Format("Filename={0}", AbataConfig.DB_PATH)))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    Dictionary<string, SqliteParameter> paramMap = songs.MapToParameter();
                    var cmd = db.CreateCommand();
                    cmd.CommandText =
                        $"INSERT INTO songs({String.Join(", ", paramMap.Keys)})"
                        + "\n" + $"VALUES ({String.Join(", ", paramMap.Keys.Select(i => "$" + i))})";
                    cmd.Parameters.AddRange(paramMap.Values);

                    foreach (Song s in songs)
                    {
                        foreach (var item in s.MapValuesToDb())
                        {
                            paramMap[item.Key].Value = item.Value;
                        }
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                db.Close();
            }
        }

        public List<string> GetSongHashes()
        {
            List<string> hashes = null;
            using (SqliteConnection db = new SqliteConnection(String.Format("Filename={0}", AbataConfig.DB_PATH)))
            {
                db.Open();
                SqliteCommand selectHash = new SqliteCommand(
                    "SELECT distinct song_hash FROM songs", db);
                SqliteDataReader query = selectHash.ExecuteReader();
                if (query.Read())
                {
                    hashes = new List<string>();
                    do
                    {
                        hashes.Add(query.GetString(0));
                    }
                    while (query.Read());
                }

                db.Close();
            }

            return hashes;
        }

        public SongSet GetSongs()
        {
            SongSet res = null;
            using (SqliteConnection db = new SqliteConnection(String.Format("Filename={0}", AbataConfig.DB_PATH)))
            {
                db.Open();
                string stmt = @"SELECT
                                    s.*
                                FROM
                                    songs s";
                SqliteCommand cmd = new SqliteCommand(stmt, db);
                SqliteDataReader query = cmd.ExecuteReader();
                if (query.Read())
                {
                    res = new SongSet();
                    do
                    {
                        res.Add(new Song(query));
                    }
                    while (query.Read());
                }
                db.Close();
            }

            return res;
        }
    }
}

