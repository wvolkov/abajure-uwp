using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abajure.Entities
{
    class LyricLine
    {
        public string Line { get; private set; }
        public TimeSpan Start { get; private set; }
        public TimeSpan End { get; private set; }

        public LyricLine(string line, TimeSpan start, TimeSpan end)
        {
            Line = line;
            Start = start;
            End = end;
        }
    }

    class LyricLineSet: ObservableCollection<LyricLine>
    {

        public LyricLineSet(IEnumerable<LyricLine> col) : base(col) { }

        public LyricLineSet(JArray jsLyrics, bool withTime)
        {
            if(withTime)
            {
                TimeSpan
                    start = TimeSpan.Zero,
                    end = TimeSpan.Zero;
                string
                    prevLine = "",
                    newLine = "";

                foreach(var lyric in jsLyrics)
                {
                    newLine = lyric["text"].Value<string>();
                    var time = lyric["time"];
                    if(time!=null)
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
        }

        public LyricLine this[TimeSpan interval]
        {
            get
            {
                return this.FirstOrDefault(l => interval >= l.Start && interval <= l.End);
            }
        }
    }
}
