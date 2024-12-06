using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Model
{
    public enum CommunicationStatus : int
    {
        Unknown = 0,
        Ok = 200,
        BadRequest = 400,
        InternalServerError = 500,
        Unauthorized = 561
    }
}
