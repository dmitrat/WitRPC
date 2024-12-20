using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Server.Rest
{
    public class RestServerTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Url: {Url}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is RestServerTransportOptions options))
                return false;

            return Url.Is(options.Url);
        }

        public override RestServerTransportOptions Clone()
        {
            return new RestServerTransportOptions
            {
                Url = Url
            };
        }

        #endregion

        #region Properties

        public string? Url { get; set; }

        #endregion
    }
}
