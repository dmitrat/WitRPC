using System;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Proxy.Interfaces;
using OutWit.Common.Values;

namespace OutWit.Common.Proxy
{
    public class ProxyInvocation : ModelBase, IProxyInvocation
    {
        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not ProxyInvocation hostInfo)
                return false;

            return MethodName.Is(hostInfo.MethodName) &&
                   Parameters.Is(hostInfo.Parameters) &&
                   ParametersTypes.Is(hostInfo.ParametersTypes) &&
                   GenericArguments.Is(hostInfo.GenericArguments) &&
                   ReturnValue.Equals(hostInfo.ReturnValue) &&
                   ReturnType.Is(hostInfo.ReturnType);
        }

        public override ModelBase Clone()
        {
            return new ProxyInvocation
            {
                MethodName = MethodName,
                Parameters = Parameters,
                ParametersTypes = ParametersTypes,
                GenericArguments = GenericArguments,
                ReturnValue = ReturnValue,
                ReturnType = ReturnType
            };
        }

        #endregion

        #region Properties

        public string MethodName { get; set; }

        public object[] Parameters { get; set; }

        public string[] ParametersTypes { get; set; }

        public string[] GenericArguments { get; set; }

        public object ReturnValue { get; set; }

        public string ReturnType { get; set; }

        #endregion
    }
}
