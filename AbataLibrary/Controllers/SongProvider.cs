using AbataLibrary.Entities;
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

        public event EventHandler ScanComplete;

        public void OnScanComplete(EventArgs e)
        {
            this.ScanComplete?.Invoke(this, e);
        }

        private SongProvider()
        {
            _orig = new SongSet();
            SongSet = _orig;
        }

        public async void ScanLib()
        {
            IsScanComplete = false;

            QueryOptions queryOption = new QueryOptions
                (CommonFileQuery.OrderByMusicProperties, new string[] { ".mp3", ".m4a" });

            queryOption.FolderDepth = FolderDepth.Deep;

            //Queue<IStorageFolder> folders = new Queue<IStorageFolder>();

            //StorageFolder bowieFolder = await KnownFolders.MusicLibrary;

            IReadOnlyList<StorageFile> files = await KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOption).GetFilesAsync();

            bool success = await _orig.AddSongSetAsync(files);

            IsScanComplete = true;

            OnScanComplete(EventArgs.Empty);
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
                _provider.ScanLib();
            }
            return _provider;
        }
    }
}
