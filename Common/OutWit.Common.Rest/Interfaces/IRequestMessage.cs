using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Rest.Interfaces
{
    public interface IRequestMessage : IRequestPost
    {
        AuthenticationHeaderValue? BuildHeader();

        HttpMethod Method { get; }
    }
}
