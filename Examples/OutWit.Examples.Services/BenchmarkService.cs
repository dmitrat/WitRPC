using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Examples.Contracts;
using OutWit.Examples.Contracts.Model;
using OutWit.Examples.Contracts.Utils;

namespace OutWit.Examples.Services
{
    public class BenchmarkService : IBenchmarkService, IBenchmarkGrpcServiceProto
    {
        public async Task<long> OneWayBenchmark(byte[] bytes)
        {
            return HashUtils.ComputeFnv1aHash(bytes);
        }

        public async Task<byte[]> TwoWaysBenchmark(byte[] bytes)
        {
            return bytes;
        }

        public async Task<BenchmarkResponse> OneWayBenchmark(BenchmarkRequest request)
        {
            if(request.Bytes == null)
                return new BenchmarkResponse { Length = 0, Bytes = null };

            return new BenchmarkResponse
            {
                Length = HashUtils.ComputeFnv1aHash(request.Bytes), 
                Bytes = null
            };
        }

        public async Task<BenchmarkResponse> TwoWaysBenchmark(BenchmarkRequest request)
        {
            if (request.Bytes == null)
                return new BenchmarkResponse { Length = 0, Bytes = null };

            return new BenchmarkResponse
            {
                Length = HashUtils.ComputeFnv1aHash(request.Bytes),
                Bytes = request.Bytes
            };
        }
    }
}
