using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.MemoryPack
{
    public class MemoryPackOptions
    {
        internal MemoryPackOptions()
        {
            
        }

        public void Register<T>(MemoryPackFormatter<T> formatter)
        {
            MemoryPackUtils.Register(formatter);
        }
    }
}
