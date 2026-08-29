using System;
using System.Linq;
using System.Reflection;
using OutWit.Communication.Client;
using OutWit.Communication.Interfaces;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Messages;
using OutWit.Communication.Server;

namespace OutWit.Communication.Tests.Packaging
{
    /// <summary>
    /// Locks in the dynamic proxy package split: the core packages must stay free
    /// of Castle.Core so the client publishes cleanly under NativeAOT, while the
    /// opt-in OutWit.Communication.Client.DynamicProxy package carries the
    /// runtime proxy path and keeps the historical namespaces for source
    /// compatibility.
    /// </summary>
    [TestFixture]
    public class DynamicProxySplitTests
    {
        private const string DYNAMIC_PROXY_ASSEMBLY = "OutWit.Communication.Client.DynamicProxy";

        [Test]
        public void CommunicationAssemblyDoesNotReferenceCastleTest()
        {
            var references = typeof(WitMessage).Assembly.GetReferencedAssemblies();

            Assert.That(references.Select(assembly => assembly.Name),
                Has.None.StartsWith("Castle"));
        }

        [Test]
        public void ClientAssemblyDoesNotReferenceCastleTest()
        {
            var references = typeof(WitClient).Assembly.GetReferencedAssemblies();

            Assert.That(references.Select(assembly => assembly.Name),
                Has.None.StartsWith("Castle"));
        }

        [Test]
        public void ServerAssemblyDoesNotReferenceCastleTest()
        {
            var references = typeof(WitServer).Assembly.GetReferencedAssemblies();

            Assert.That(references.Select(assembly => assembly.Name),
                Has.None.StartsWith("Castle"));
        }

        [Test]
        public void DynamicProxyAssemblyReferencesCastleTest()
        {
            var references = typeof(RequestInterceptorDynamic).Assembly.GetReferencedAssemblies();

            Assert.That(references.Select(assembly => assembly.Name),
                Has.Some.EqualTo("Castle.Core"));
        }

        [Test]
        public void DynamicProxyTypesLiveInDynamicProxyAssemblyTest()
        {
            Assert.That(typeof(RequestInterceptorDynamic).Assembly.GetName().Name,
                Is.EqualTo(DYNAMIC_PROXY_ASSEMBLY));

            Assert.That(typeof(WitClientDynamicProxyExtensions).Assembly.GetName().Name,
                Is.EqualTo(DYNAMIC_PROXY_ASSEMBLY));
        }

        [Test]
        public void DynamicGetServiceExtensionKeepsClientNamespaceTest()
        {
            Assert.That(typeof(WitClientDynamicProxyExtensions).Namespace,
                Is.EqualTo("OutWit.Communication.Client"));

            // One overload for the persistent client, one for any IClient (the
            // stateless REST client in particular, since 3.1.1).
            var receivers = typeof(WitClientDynamicProxyExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "GetService")
                .Select(method => method.GetParameters().First().ParameterType)
                .ToArray();

            Assert.That(receivers, Is.EquivalentTo(new[] { typeof(WitClient), typeof(IClient) }));
        }
    }
}
