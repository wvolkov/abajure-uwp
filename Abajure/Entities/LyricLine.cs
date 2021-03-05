using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Abajure.Entities
{
    [DataContract]
    class LyricLine
    {
        [DataMember]
        public string Line { get; private set; }
        [DataMember]
        public TimeSpan Start { get; private set; }
        [DataMember]
        public TimeSpan End { get; private set; }

        public LyricLine(string line, TimeSpan start, TimeSpan end)
        {
            Line = line.Trim();
            Start = start;
            End = end;
        }

        public LyricLine(string line)
        {
            Line = line.Trim();
        }
    }

    [CollectionDataContract]
    class LyricLineSet : ObservableCollection<LyricLine>
    {
        public bool Timed { get { return this.FirstOrDefault(i => i.End != TimeSpan.Zero) != null; }}

        public LyricLineSet() : base() { }

        public LyricLineSet(IEnumerable<LyricLine> col) : base(col) { }

        public LyricLineSet(string lyrics)
        {
            var lyricLines = lyrics.Split('\n');
            foreach (var line in lyricLines)
                this.Add(new LyricLine(line));
        }

        public LyricLineSet(JArray jsLyrics)
        {
            TimeSpan
                start = TimeSpan.Zero,
                end = TimeSpan.Zero;
            string
                prevLine = "",
                newLine = "";

            foreach (var lyric in jsLyrics)
            {
                newLine = lyric["text"].Value<string>();
                var time = lyric["time"];
                if (time != null)
                {
                    int min = time["minutes"].Value<int>(),
                        sec = time["seconds"].Value<int>(),
                        ms = time["hundredths"].Value<int>();
                    end = new TimeSpan(0, 0, min, sec, ms * 10);
                    if (prevLine != "")
                    {
                        this.Add(new LyricLine(prevLine, start, end));
                    }
                    start = end;
                    prevLine = newLine;
                }
            }
            this.Add(new LyricLine(newLine, start, end));
        }

        public LyricLine this[TimeSpan interval]
        {
            get
            {
                return this.FirstOrDefault(l => interval >= l.Start && interval <= l.End);
            }
        }

        public async void Save(string fileName)
        {
            IBuffer buffMsg;

            DataContractSerializer sessionSerializer = new DataContractSerializer(typeof(LyricLineSet));
            using (var stream = new MemoryStream())
            {
                sessionSerializer.WriteObject(stream, this);

                buffMsg = stream.ToArray().AsBuffer();
            }
            var lyricsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("lyrics");
            StorageFile sessionFile = await lyricsFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBufferAsync(sessionFile, buffMsg);
        }

        public static async Task<LyricLineSet> Load(string fileName)
        { 
            try
            {
                DataContractSerializer sessionSerializer = new DataContractSerializer(typeof(LyricLineSet));
                var lyricsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("lyrics");
                var lyricFile = lyricsFolder.GetItemAsync(fileName);
                if (lyricFile != null)
                    using (var sessionFileStream = await lyricsFolder.OpenStreamForReadAsync(fileName))
                    {
                        var obj = sessionSerializer.ReadObject(sessionFileStream);
                        if (obj != null)
                            return (LyricLineSet)obj;
                    }
            }
            catch { }
            return null;
        }
    }
}
