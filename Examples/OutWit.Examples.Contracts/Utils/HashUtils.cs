using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Contracts.Utils
{
    public static class HashUtils
    {
        public static long ComputeFnv1aHash(byte[] data)
        {
            const long fnvPrime = 16777619;
            const long offsetBasis = 2166136261;

            long hash = offsetBasis;
            foreach (byte b in data)
            {
                hash ^= b;
                hash *= fnvPrime;
            }
            return hash;
        }

        public static long FastHash(byte[] data)
        {
            return BitConverter.ToInt64(new byte[]
                { data[0], data[1], data[2], data[3], data[^1], data[^2], data[^3], data[^4] });
        }
    }
}
