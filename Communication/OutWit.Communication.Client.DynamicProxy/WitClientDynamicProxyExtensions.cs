using System;
using Castle.DynamicProxy;
using OutWit.Communication.Interceptors;
using OutWit.Communication.Interfaces;

namespace OutWit.Communication.Client
{
    /// <summary>
    /// Runtime dynamic proxy support for <see cref="WitClient"/>.
    /// Lives in its own package so that the base client stays free of Castle.Core
    /// and publishes cleanly under NativeAOT/trimming. The namespace matches
    /// <see cref="WitClientBuilder"/> on purpose: existing callers of
    /// GetService&lt;TService&gt;() recompile unchanged after adding a reference
    /// to OutWit.Communication.Client.DynamicProxy.
    /// </summary>
    public static class WitClientDynamicProxyExtensions
    {
        public static TService GetService<TService>(this WitClient me, bool strongAssemblyMatch = true)
            where TService : class
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(me, strongAssemblyMatch, typeof(TService));

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<TService>(interceptor);
        }

        /// <summary>
        /// The same for any <see cref="IClient"/> -- the stateless REST client
        /// (<c>WitClientRest</c>) in particular, which lives in its own package.
        /// </summary>
        public static TService GetService<TService>(this IClient me, bool strongAssemblyMatch = true)
            where TService : class
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new RequestInterceptorDynamic(me, strongAssemblyMatch, typeof(TService));

            return proxyGenerator.CreateInterfaceProxyWithoutTarget<TService>(interceptor);
        }
    }
}
