using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Proxy.Interfaces
{
    public interface IProxyInvocation
    {
        public string MethodName { get; }

        public object[] Parameters { get; }

        public string[] ParametersTypes { get; }

        public string[] GenericArguments { get; }

        public bool HasReturnValue { get; }

        public object ReturnValue { get; set; }

        public string ReturnType { get; }

        public bool ReturnsTask { get; }

        public bool ReturnsTaskWithResult { get; }

        public string TaskResultType { get; }
    }
}
