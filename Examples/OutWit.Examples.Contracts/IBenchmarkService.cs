using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using OutWit.Examples.Contracts.Model;

namespace OutWit.Examples.Contracts
{
    [CoreWCF.ServiceContract]
    [ServiceContract]
    public interface IBenchmarkService
    {
        [CoreWCF.OperationContract]
        [OperationContract]
        public Task<long> OneWayBenchmark(byte[] bytes);

        [CoreWCF.OperationContract]
        [OperationContract]
        public Task<byte[]> TwoWaysBenchmark(byte[] bytes);
    }

    [CoreWCF.ServiceContract]
    [ServiceContract]
    public interface IBenchmarkGrpcServiceProto
    {
        [CoreWCF.OperationContract]
        [OperationContract]
        public Task<BenchmarkResponse> OneWayBenchmark(BenchmarkRequest request);

        [CoreWCF.OperationContract]
        [OperationContract]
        public Task<BenchmarkResponse> TwoWaysBenchmark(BenchmarkRequest request);
    }
}
