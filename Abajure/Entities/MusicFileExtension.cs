using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abajure.Entities
{
    static class MusicFileExtension
    {
        public static MusicFiles OrderByProperty<TKey>(this MusicFiles mList, Func<MusicFile, TKey> func, MusicFileSortOrder ord)
        {
            switch(ord)
            {
                case MusicFileSortOrder.Ascending:
                    return new MusicFiles(mList.OrderBy(func));
                case MusicFileSortOrder.Descending:
                    return new MusicFiles(mList.OrderByDescending(func));
                default:
                    return null;
            }            
        }
    }

    public enum MusicFileSortOrder
    {
        Ascending   = 0,
        Descending  = 1
    }
}
