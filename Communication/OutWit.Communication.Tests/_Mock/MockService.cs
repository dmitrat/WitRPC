using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.Aspects.Utils;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Mock
{
    public class MockService : IService
    {

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public event ServiceProgressEventHandler Progress = delegate { };

        public event ServiceStringEventHandler Error = delegate { };

        public event ServiceComplexEventHandler<int, int> StartProcessingRequested = delegate { };


        public event EventHandler<ComplexNumber<int, string>> GeneralEvent = delegate { };

        public MockService()
        {
            StringProperty = "TestString";
            DoubleProperty = 1.2;
        }


        public string RequestData(string message)
        {
            GeneralEvent(this, new ComplexNumber<int, string>(1, message));

            return message;
        }

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

        public T GenericSimple<T>(int number, string text, T value)
        {
            return value;
        }

        public ComplexNumber<T1, T2> GenericComplex<T1, T2>(int number, string text, ComplexNumber<T1, T2> value)
        {
            return value;
        }

        public ComplexNumber<T1, T2> GenericComplexArray<T1, T2>(int number, string text, List<ComplexNumber<T1, T2>> value)
        {
            return value.First();
        }

        public ComplexNumber<T2, T3> GenericComplexMulti<T1, T2, T3, T4>(ComplexNumber<T1, T2> num, string text, List<ComplexNumber<T3, T4>> value)
        {
            return new ComplexNumber<T2, T3>(num.B, value.First().A);
        }


        public void ReportError(string error)
        {
            Error(error);
        }

        public string StringProperty { get; }

        [Notify]
        public double DoubleProperty { get; set; }

    }
}
