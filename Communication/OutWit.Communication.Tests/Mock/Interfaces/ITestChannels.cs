using System;
using System.Threading.Tasks;

namespace OutWit.Communication.Tests.Mock.Interfaces
{
    /// <summary>
    /// Test channel 1 interface for composite service tests.
    /// </summary>
    public interface ITestChannel1
    {
        string GetChannel1Data(string input);
        Task<int> Channel1AsyncMethod(int value);
    }

    /// <summary>
    /// Test channel 2 interface for composite service tests.
    /// </summary>
    public interface ITestChannel2
    {
        string GetChannel2Data(string input);
        double Channel2Calculate(double a, double b);
        Task<string> Channel2AsyncMethod(string value);
    }
}
