using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Interfaces
{
    public interface IValueConverter
    {
        bool TryConvert(object? origValue, Type destType, out object? destValue);
    }
}
