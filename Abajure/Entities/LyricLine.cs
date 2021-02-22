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

        public LyricLine this[TimeSpan interval]
        {
            get
            {
                return this.FirstOrDefault(l => interval >= l.Start && interval <= l.End);
            }
        }
    }
}
