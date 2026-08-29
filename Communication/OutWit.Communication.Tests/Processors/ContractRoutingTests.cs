using OutWit.Communication.Model;
using OutWit.Communication.Processors;
using OutWit.Communication.Requests;
using OutWit.Communication.Serializers;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Processors
{
    /// <summary>
    /// Contract-scoped ids of 3.0: deterministic, assembly-independent, and the
    /// thing that makes two services with identical method signatures routable
    /// on one channel -- the misrouting the audit reported (finding 7).
    /// </summary>
    [TestFixture]
    public sealed class ContractRoutingTests
    {
        #region Id Tests

        [Test]
        public void StableNameRendersWithoutAssemblyIdentityTest()
        {
            Assert.That(ContractIds.StableName(typeof(string)), Is.EqualTo("System.String"));
            Assert.That(ContractIds.StableName(typeof(int[])), Is.EqualTo("System.Int32[]"));
            Assert.That(ContractIds.StableName(typeof(List<int>)), Is.EqualTo("System.Collections.Generic.List<System.Int32>"));
            Assert.That(
                ContractIds.StableName(typeof(Dictionary<string, List<int>>)),
                Is.EqualTo("System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.List<System.Int32>>"));
        }

        [Test]
        public void MethodIdIsDeterministicTest()
        {
            var first = ContractIds.GetMethodId(typeof(IFirstService), typeof(IFirstService).GetMethod(nameof(IFirstService.CancelJob))!);
            var again = ContractIds.GetMethodId(typeof(IFirstService), nameof(IFirstService.CancelJob), new[] { typeof(Guid) });

            Assert.That(first, Is.Not.EqualTo(ContractIds.NONE));
            Assert.That(again, Is.EqualTo(first));
        }

        [Test]
        public void SameSignatureOnDifferentContractsGetsDifferentIdsTest()
        {
            var first = ContractIds.GetMethodId(typeof(IFirstService), nameof(IFirstService.CancelJob), new[] { typeof(Guid) });
            var second = ContractIds.GetMethodId(typeof(ISecondService), nameof(ISecondService.CancelJob), new[] { typeof(Guid) });

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void GenericMethodGetsNoIdTest()
        {
            var method = typeof(IGenericService).GetMethod(nameof(IGenericService.Echo))!;

            Assert.That(ContractIds.GetMethodId(typeof(IGenericService), method), Is.EqualTo(ContractIds.NONE));
        }

        #endregion

        #region Routing Tests

        [Test]
        public async Task SameSignatureRoutesToTheRightServiceByMethodIdTest()
        {
            var first = new FirstService();
            var second = new SecondService();

            var processor = new CompositeRequestProcessor()
                .Register<IFirstService>(first)
                .Register<ISecondService>(second);

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var jobId = Guid.NewGuid();

            // The audited defect: by name and parameters alone these two calls
            // are indistinguishable, and first-registration used to win both.
            var response = await processor.Process(CancelRequest<ISecondService>(jobId, serializer));
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));

            Assert.That(second.Cancelled, Is.EqualTo(jobId));
            Assert.That(first.Cancelled, Is.Null);

            response = await processor.Process(CancelRequest<IFirstService>(jobId, serializer));
            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(first.Cancelled, Is.EqualTo(jobId));
        }

        [Test]
        public async Task RequestWithoutMethodIdFallsBackToNameRoutingTest()
        {
            var service = new FirstService();

            var processor = new CompositeRequestProcessor()
                .Register<IFirstService>(service);

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var request = nameof(IFirstService.CancelJob)
                .CreateRequest(new object?[] { Guid.NewGuid() }, new[] { typeof(Guid) }, serializer);
            request.ParameterTypesByName = new[] { new ParameterType(typeof(Guid)) };

            var response = await processor.Process(request);

            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(service.Cancelled, Is.Not.Null);
        }

        [Test]
        public async Task SingleProcessorDispatchesByMethodIdTest()
        {
            var service = new FirstService();
            var processor = new RequestProcessor<IFirstService>(service);

            var serializer = new MessageSerializerJson();
            processor.ResetSerializer(serializer);

            var jobId = Guid.NewGuid();
            var response = await processor.Process(CancelRequest<IFirstService>(jobId, serializer));

            Assert.That(response.Status, Is.EqualTo(CommunicationStatus.Ok));
            Assert.That(service.Cancelled, Is.EqualTo(jobId));
        }

        #endregion

        #region Helpers

        private static WitRequest CancelRequest<TContract>(Guid jobId, MessageSerializerJson serializer)
        {
            // Only the id names the target -- no parameter type information at
            // all, which the legacy path could not have dispatched.
            var request = "CancelJob".CreateRequest(new object?[] { jobId }, new[] { typeof(Guid) }, serializer);
            request.ContractId = ContractIds.GetContractId(typeof(TContract));
            request.MethodId = ContractIds.GetMethodId(typeof(TContract), "CancelJob", new[] { typeof(Guid) });
            return request;
        }

        #endregion

        #region Mock Contracts

        public interface IFirstService
        {
            void CancelJob(Guid jobId);
        }

        public interface ISecondService
        {
            void CancelJob(Guid jobId);
        }

        public interface IGenericService
        {
            T Echo<T>(T value);
        }

        private sealed class FirstService : IFirstService
        {
            public Guid? Cancelled { get; private set; }

            public void CancelJob(Guid jobId) => Cancelled = jobId;
        }

        private sealed class SecondService : ISecondService
        {
            public Guid? Cancelled { get; private set; }

            public void CancelJob(Guid jobId) => Cancelled = jobId;
        }

        #endregion
    }
}
