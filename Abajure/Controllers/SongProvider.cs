using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace Abajure.Entities
{
    class SongProvider
    {
        SongSet _orig;
        public SongSet SongSet { get; private set; }

        public SongProvider()
        {
            _orig = new SongSet();
            SongSet = _orig;
        }

        public async void ScanLib()
        {
            QueryOptions queryOption = new QueryOptions
                (CommonFileQuery.OrderByMusicProperties, new string[] { ".mp3", ".m4a" });

            queryOption.FolderDepth = FolderDepth.Deep;

            //Queue<IStorageFolder> folders = new Queue<IStorageFolder>();

            //StorageFolder bowieFolder = await KnownFolders.MusicLibrary;

            var files = await KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOption).GetFilesAsync();

            _orig.AddSongSetAsync(files);
        }

        public void Search(string strPat)
        {
            if (strPat != "")
                SongSet = new SongSet(_orig.Where(i => i.Title.Contains(strPat)));
            else
                SongSet = _orig;
        }
    }
}
