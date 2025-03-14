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
    }
}
