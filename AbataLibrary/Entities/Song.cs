using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace AbataLibrary.Entities
{
    public class Song
    {
        public string SongPath { get; private set; }
        public DateTimeOffset DateModified { get; private set; }
        public string Album { get; private set; }
        public string AlbumArtist { get; private set; }
        public string Artist { get; private set; }
        public uint Bitrate { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public uint TrackNumber { get; private set; }
        public uint Year { get; private set; }
        public MediaPlaybackItem MediaPlaybackItem { get; private set;}

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
                MediaPlaybackItem = pbi
            };

            return song;
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
    }

    public class SongSet : ObservableCollection<Song>
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
    }
}
