using System;
using System.ComponentModel;
using OutWit.Common.Aspects;
using OutWit.Communication.Tests._Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Mock
{
    public class MockService : IService
    {
        #region Events

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public event ServiceProgressEventHandler Progress = delegate { };

        public event ServiceStringEventHandler Error = delegate { };

        public event ServiceComplexEventHandler<int, int> StartProcessingRequested = delegate { };


        public event EventHandler<ComplexNumber<int, string>> GeneralEvent = delegate { };

        #endregion

        #region Constructors

        public MockService()
        {
            StringProperty = "TestString";
            DoubleProperty = 1.2;
        }

        #endregion

        #region RequestData

        public string RequestData(string message)
        {
            GeneralEvent(this, new ComplexNumber<int, string>(1, message));

            return message;
        }

        public async Task<string> RequestDataAsync(string message)
        {
            return await Task.Run(() => RequestData(message));
        }

        #endregion

        #region StartProcessing

        public ComplexNumber<int, int> StartProcessing(ComplexNumber<int, int> number, int iterations)
        {
            StartProcessingRequested(number, iterations);

            int a = number.A;
            int b = number.B;
            for (int i = 0; i < iterations; i++)
            {
                Thread.Sleep(100);
                a += a;
                b += b;

                Progress((int)Math.Ceiling(i / (double)iterations) * 100);
            }

            return new ComplexNumber<int, int>(a, b);
        }

        public async Task<ComplexNumber<int, int>> StartProcessingAsync(ComplexNumber<int, int> number, int iterations)
        {
            return await Task.Run(() => StartProcessing(number, iterations));
        }

        #endregion

        #region GenericSimple

        public T GenericSimple<T>(int number, string text, T value)
        {
            return value;
        }

        public Task<T> GenericSimpleAsync<T>(int number, string text, T value)
        {
            return Task.Run(() => GenericSimple(number, text, value));
        }

        #endregion

        #region GenericComplex

        public ComplexNumber<T1, T2> GenericComplex<T1, T2>(int number, string text, ComplexNumber<T1, T2> value)
        {
            return value;
        }

        public async Task<ComplexNumber<T1, T2>> GenericComplexAsync<T1, T2>(int number, string text, ComplexNumber<T1, T2> value)
        {
            return await Task.Run(() => GenericComplex(number, text, value));
        }

        #endregion

        #region GenericCoplexArray

        public async Task<ComplexNumber<T1, T2>> GenericComplexArrayAsync<T1, T2>(int number, string text, List<ComplexNumber<T1, T2>> value)
        {
            return await Task.Run(() => GenericComplexArray(number, text, value));
        }

        public ComplexNumber<T1, T2> GenericComplexArray<T1, T2>(int number, string text, List<ComplexNumber<T1, T2>> value)
        {
            return value.First();
        }

        #endregion

        #region GenericComplexMulti

        public async Task<ComplexNumber<T2, T3>> GenericComplexMultiAsync<T1, T2, T3, T4>(ComplexNumber<T1, T2> number, string text, List<ComplexNumber<T3, T4>> value)
        {
            return await Task.Run(() => GenericComplexMulti(number, text, value));
        }

        public ComplexNumber<T2, T3> GenericComplexMulti<T1, T2, T3, T4>(ComplexNumber<T1, T2> num, string text, List<ComplexNumber<T3, T4>> value)
        {
            return new ComplexNumber<T2, T3>(num.B, value.First().A);
        }

        #endregion


        #region ReportError

        public void ReportError(string error)
        {
            Error(error);
        }

        public async Task ReportErrorAsync(string error)
        {
            await Task.Run(() => ReportError(error));
        }

        #endregion

        #region Properies

        public string StringProperty { get; }

        [Notify]
        public double DoubleProperty { get; set; }

        #endregion

    }
}
