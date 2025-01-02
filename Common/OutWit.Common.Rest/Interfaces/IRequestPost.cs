using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Rest.Interfaces
{
    public interface IRequestPost : IRequest
    {
        HttpContent? BuildContent();
    }
}
