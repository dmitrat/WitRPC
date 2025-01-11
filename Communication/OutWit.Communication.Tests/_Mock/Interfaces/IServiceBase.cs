using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Proxy.Attributes;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests._Mock.Interfaces
{
    [ProxyTarget("ServiceProxy")]
    public interface IServiceBase : INotifyPropertyChanged
    {
        public event ServiceProgressEventHandler Progress;

        public event ServiceStringEventHandler Error;

        public event ServiceComplexEventHandler<int, int> StartProcessingRequested;

        public event EventHandler<ComplexNumber<int, string>> GeneralEvent;


        public string RequestData(string message);

        public ComplexNumber<int, int> StartProcessing(ComplexNumber<int, int> number, int iterations);

        public void ReportError(string error);


        public string StringProperty { get; }

        public double DoubleProperty { get; set; }
    }

    public delegate void ServiceProgressEventHandler(int progress);

    public delegate void ServiceStringEventHandler(string message);

    public delegate void ServiceComplexEventHandler<T1, T2>(ComplexNumber<T1, T2> number, int iterations);
}
