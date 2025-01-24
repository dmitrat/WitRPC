using System;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Interfaces
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message, ILogger? logger = null) where T : class;

        T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T : class;
    }
}
