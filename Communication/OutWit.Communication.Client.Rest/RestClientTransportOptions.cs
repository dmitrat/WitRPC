using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Model;

namespace OutWit.Communication.Client.Rest
{
    public class RestClientTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Host: {Host}, Mode: {Mode}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is RestClientTransportOptions options))
                return false;

            return Host.Check(options.Host)
                   && Mode.Is(options.Mode);
        }

        public override RestClientTransportOptions Clone()
        {
            return new RestClientTransportOptions
            {
                Host = Host?.Clone(),
                Mode = Mode,
            };
        }

        #endregion

        #region Properties

        public HostInfo? Host { get; set; }

        public RestClientRequestModes Mode { get; set; }

        #endregion
    }

    public enum RestClientRequestModes
    {
        PostOnly = 0,
        AllowGetForMethodsWithoutParameters,
        AllowGetForMethodsWithSingleParameter,
        AllowGet
    }
}
