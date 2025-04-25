using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Examples.Contracts.Model
{
    [ProtoContract]
    public class BenchmarkResponse
    {
        [ProtoMember(1)]
        public long Length { get; set; }

        [ProtoMember(2)]
        public byte[]? Bytes { get; set; }
    }
}
