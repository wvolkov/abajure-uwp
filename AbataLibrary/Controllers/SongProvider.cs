using AbataLibrary.Entities;
using AbataLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace AbataLibrary.Controllers
{
    public class SongProvider
    {
        SongSet _orig;
        public SongSet SongSet { get; private set; }
        public bool IsScanComplete { get; private set; }

        public event SongScanEventHandler ScanComplete;

        public void OnScanComplete(SongScanEventArgs e)
        {
            this.ScanComplete?.Invoke(this, e);
        }

        private SongProvider()
        {
            AbataProvider aba = AbataProvider.GetProvider();
            var songs = aba.GetSongs();
            if (songs == null)
            {
                songs = new SongSet();
            }
            _orig = songs;
            SongSet = songs;
        }

        public async void ScanLib()
        {
            IsScanComplete = false;

            QueryOptions queryOption = new QueryOptions
                (CommonFileQuery.OrderByMusicProperties, new string[] { ".mp3", ".m4a" });

            queryOption.FolderDepth = FolderDepth.Deep;

            //Queue<IStorageFolder> folders = new Queue<IStorageFolder>();

            //StorageFolder bowieFolder = await KnownFolders.MusicLibrary;

            var files = await KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOption).GetFilesAsync();
            AbataProvider aba = AbataProvider.GetProvider();
            SongSet newSongs = new SongSet();
            var currentSongsHashes = _orig.Select(u => u.Hash).Distinct();
            foreach (var hashRes in Hasher.GetHash(files))
            {
                string hash = await hashRes.task;
                if (!currentSongsHashes.Contains(hash))
                {
                    Song song = await Song.CreateSongAsync(hashRes.file);
                    newSongs.Add(song);
                    _orig.Add(song);
                    aba.InsertSongs(new SongSet() { song });
                }
            }

            IsScanComplete = true;

            OnScanComplete(new SongScanEventArgs(newSongs));
        }

    public void Search(string strPat)
    {
        if (strPat != "")
            SongSet = new SongSet(_orig.Where(i => i.Title.ToLower().Contains(strPat.ToLower())
                                                    || i.Artist.ToLower().Contains(strPat.ToLower())
                                                    || i.Album.ToLower().Contains(strPat.ToLower())));
        else
            SongSet = _orig;
    }

    private static SongProvider _provider;

    public static SongProvider GetSongProvider()
    {
        if (_provider == null)
        {
            _provider = new SongProvider();
        }
        return _provider;
    }
}

public delegate void SongScanEventHandler(object sender, SongScanEventArgs e);

public class SongScanEventArgs : EventArgs
{
    public SongSet Songs { get; private set; }

    public SongScanEventArgs(SongSet songs)
    {
        Songs = songs;
    }
}
}
