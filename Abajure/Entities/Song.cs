using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Abajure.Entities
{
    class Song
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
                Year = mp.Year
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

    class SongSet : ObservableCollection<Song>
    {
        public SongSet() { }
        public SongSet(IEnumerable<Song> col)
            : base(col) { }

        private async static void AddSongSetAsync(SongSet songSet, IReadOnlyList<StorageFile> files)
        {
            foreach (StorageFile file in files)
                songSet.Add(await Song.CreateSongAsync(file));
        }

        public async void AddSongSetAsync(IReadOnlyList<StorageFile> files)
        {
            foreach (StorageFile file in files)
                this.Add(await Song.CreateSongAsync(file));
        }

        public async Task<MediaPlaybackList> AsMediaPlayBackListAsync()
        {
            MediaPlaybackList res = new MediaPlaybackList();
            foreach(var song in this)
            {
                var songFile = await song.AsStorageFileAsync();
                res.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromStorageFile(songFile)));
            }
            return res;
        }

        public SongSet GetSongsAfter(Song s)
        {
            int inx = this.IndexOf(s);
            if (inx >= 0)
                return new SongSet(this.Skip(inx + 1).Take(10));
            else
                return null;
        }
    }
}
