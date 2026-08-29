using OutWit.Communication.Tests.Mock.Interfaces;

namespace OutWit.Communication.Tests.Mock
{
    public class MockEchoService : IEchoService
    {
        #region IEchoService

        public string EchoText(string text)
        {
            return $"echo: {text}";
        }

        public int SumNumbers(int a, int b)
        {
            return a + b;
        }

        #endregion
    }
}
