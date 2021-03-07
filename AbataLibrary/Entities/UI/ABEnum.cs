using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbataLibrary.Entities.UI
{
    public enum ABEnum
    {
        Released = 1 << 1,
        APressed = 1 << 2,
        BPressed = 1 << 3
    }
}
