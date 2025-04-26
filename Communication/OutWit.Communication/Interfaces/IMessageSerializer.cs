using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace OutWit.Communication.Interfaces
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message, ILogger? logger = null) where T : class;

        byte[] Serialize(object message, Type type, ILogger? logger = null);
        

        T? Deserialize<T>(byte[] bytes, ILogger? logger = null) where T : class;

        object? Deserialize(byte[] bytes, Type type, ILogger? logger = null);
    }
}
