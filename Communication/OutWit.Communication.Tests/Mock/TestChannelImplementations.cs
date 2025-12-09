using System;
using System.Threading.Tasks;
using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Mock
{
    /// <summary>
    /// Test channel 1 implementation for composite service tests.
    /// </summary>
    public class TestChannel1Impl : ITestChannel1
    {
        public string GetChannel1Data(string input) => $"Channel1:{input}";
        
        public Task<int> Channel1AsyncMethod(int value) => Task.FromResult(value * 2);
    }

    /// <summary>
    /// Test channel 2 implementation for composite service tests.
    /// </summary>
    public class TestChannel2Impl : ITestChannel2
    {
        public string GetChannel2Data(string input) => $"Channel2:{input}";
        
        public double Channel2Calculate(double a, double b) => a + b;
        
        public Task<string> Channel2AsyncMethod(string value) => Task.FromResult($"Async:{value}");
    }
}
