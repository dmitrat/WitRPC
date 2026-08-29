namespace OutWit.Communication.Tests.Mock.Interfaces
{
    /// <summary>
    /// A second, unrelated contract for composite-host tests.
    /// </summary>
    public interface IEchoService
    {
        string EchoText(string text);

        int SumNumbers(int a, int b);
    }
}
