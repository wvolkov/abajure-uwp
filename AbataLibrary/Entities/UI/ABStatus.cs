using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbataLibrary.Entities.UI
{
    public static class ABStatus
    {
        static ABEnum _value = ABEnum.Released;

        public static ABEnum GetNextStatus()
        {
            switch(_value)
            {
                case ABEnum.Released:
                    _value = ABEnum.APressed;
                    return _value;
                case ABEnum.APressed:
                    _value = ABEnum.BPressed;
                    return _value;
                case ABEnum.BPressed:
                    _value = ABEnum.Released;
                    return _value;
                default:
                    return _value;
            }
        }

        public static void Reset()
        {
            _value = ABEnum.Released;
        }


    }
}
