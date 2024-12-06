using System;

namespace OutWit.Communication.Interfaces
{
    public interface IMessageSerializer
    {
        byte[] Serialize<T>(T message) where T : class;

        T? Deserialize<T>(byte[] bytes) where T : class;
    }
}
