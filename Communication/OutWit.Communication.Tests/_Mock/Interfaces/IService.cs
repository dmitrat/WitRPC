using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Mock.Interfaces
{
    public interface IService: INotifyPropertyChanged
    {
        public event ServiceProgressEventHandler Progress;

        public event ServiceStringEventHandler Error;

        public event ServiceComplexEventHandler<int, int> StartProcessingRequested;

        public event EventHandler<ComplexNumber<int, string>> GeneralEvent;


        public string RequestData(string message);

        public ComplexNumber<int, int> StartProcessing(ComplexNumber<int, int> number, int iterations);

        public void ReportError(string error);

        public T GenericSimple<T>(int number, string text, T value);

        public ComplexNumber<T1, T2> GenericComplex<T1, T2>(int number, string text, ComplexNumber<T1, T2> value);

        public ComplexNumber<T1, T2> GenericComplexArray<T1, T2>(int number, string text, List<ComplexNumber<T1, T2>> value);

        public ComplexNumber<T2, T3> GenericComplexMulti<T1, T2, T3, T4>(ComplexNumber<T1, T2> num, string text, List<ComplexNumber<T3, T4>> value);


        public string StringProperty { get; }

        public double DoubleProperty { get; set; }
    }

    public delegate void ServiceProgressEventHandler(int progress);

    public delegate void ServiceStringEventHandler(string message);

    public delegate void ServiceComplexEventHandler<T1, T2>(ComplexNumber<T1, T2> number, int iterations);


}
