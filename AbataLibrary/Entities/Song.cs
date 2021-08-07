using AbataLibrary.Helpers;
using AbataLibrary.Entities.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace AbataLibrary.Entities
{
    [SqliteTable("song")]
    public class Song : ISqliteValuesMapper
    {
        [SqliteTableField("id")]
        public int ID { get; private set; }
        [SqliteTableField("song_path")]
        public string SongPath { get; private set; }
        [SqliteTableField("song_hash")]
        public string Hash { get; private set; }
        [SqliteTableField("title")]
        public string Title { get; private set; }
        [SqliteTableField("artist")]
        public string Artist { get; private set; }
        [SqliteTableField("album")]
        public string Album { get; private set; }
        [SqliteTableField("album_artist")]
        public string AlbumArtist { get; private set; }
        [SqliteTableField("subtitle")]
        public string Subtitle { get; private set; }
        [SqliteTableField("bitrate")]
        public uint Bitrate { get; private set; }
        [SqliteTableField("date_modified")]
        public DateTimeOffset DateModified { get; private set; }
        [SqliteTableField("duration")]
        public TimeSpan Duration { get; private set; }
        [SqliteTableField("track_number")]
        public uint TrackNumber { get; private set; }
        [SqliteTableField("year")]
        public uint Year { get; private set; }
        public MediaPlaybackItem MediaPlaybackItem { get; private set; }


        public string DurationString
        {
            get
            {
                int h = Duration.Hours;
                return Duration.ToString(h > 0 ? @"hh\:mm\:ss" : @"mm\:ss");
            }
        }

        public static async Task<Song> CreateSongAsync(StorageFile songFile)
        {
            MusicProperties mp = await songFile.Properties.GetMusicPropertiesAsync();
            BasicProperties bp = await songFile.GetBasicPropertiesAsync();
            string fileHash = await Hasher.GetHash(songFile);
            MediaPlaybackItem pbi = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(songFile));

            Song song = new Song()
            {
                SongPath = songFile.Path,
                DateModified = bp.DateModified,
                Album = mp.Album,
                AlbumArtist = mp.AlbumArtist,
                Artist = mp.Artist,
                Bitrate = mp.Bitrate,
                Duration = mp.Duration,
                Title = mp.Title,
                Subtitle = mp.Subtitle,
                TrackNumber = mp.TrackNumber,
                Year = mp.Year,
                MediaPlaybackItem = pbi,
                Hash = fileHash
            };

            return song;
        }

        public Song() { }

        public Song(SqliteDataReader row)
        {
            PropertyInfo[] properies = typeof(Song).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach(var prop in properies)
            {
                foreach( var attr in prop.GetCustomAttributes(true))
                {
                    var attrField = attr as SqliteTableField;
                    if(attrField != null)
                    {
                        object val = null;
                        if(prop.PropertyType == typeof(DateTimeOffset))
                        {
                            val = DateTimeOffset.Parse(row[attrField.Name].ToString());
                        }
                        else if(prop.PropertyType == typeof(TimeSpan))
                        {
                            val = TimeSpan.Parse(row[attrField.Name].ToString());
                        }
                        else
                        {
                            val = Convert.ChangeType(row[attrField.Name], prop.PropertyType);
                        }
                        prop.SetValue(this, val);
                        break;
                    }
                }
            }
        }


        public async Task<StorageFile> AsStorageFileAsync()
        {
            return await StorageFile.GetFileFromPathAsync(SongPath);
        }

        public async Task<StorageItemThumbnail> GetThumbNail()
        {
            StorageFile file = await this.AsStorageFileAsync();
            StorageItemThumbnail thumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 500);
            return thumb;
        }

        public Dictionary<string, object> MapValuesToDb()
        {
            return new Dictionary<string, object> {
                    {"song_path", SongPath },
                    {"song_hash", Hash},
                    {"title", Title},
                    {"artist", Artist},
                    {"album", Album},
                    {"album_artist", AlbumArtist},
                    {"subtitle", Subtitle},
                    {"track_number", TrackNumber},
                    {"year", Year},
                    {"bitrate", Bitrate},
                    {"date_modified", DateModified},
                    {"duration", Duration } };
        }
    }

    public class SongSet : ObservableCollection<Song>, ISqliteParameterMapper
    {
        public SongSet() { }
        public SongSet(IEnumerable<Song> col)
            : base(col) { }

        private async static void AddSongSetAsync(SongSet songSet, IReadOnlyList<StorageFile> files)
        {
            foreach (StorageFile file in files)
                songSet.Add(await Song.CreateSongAsync(file));
        }

        public async Task<bool> AddSongSetAsync(IReadOnlyList<StorageFile> files)
        {
            foreach (StorageFile file in files)
                this.Add(await Song.CreateSongAsync(file));

            return true;
        }

        public Dictionary<string, SqliteParameter> MapToParameter()
        {
            Dictionary<string, SqliteParameter> res = new Dictionary<string, SqliteParameter>();
            var songDict = (new Song()).MapValuesToDb();
            foreach (var item in songDict)
            {
                res.Add(item.Key, new SqliteParameter() { ParameterName = item.Key });
            }
            return res;
        }
    }
}
