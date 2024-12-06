using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Communication.Tests.Mock.Interfaces
{
    public interface IComplexType
    {
        public int GetValue();

        public IComplexType Create(int intValue, string stringValue);


        int Int32Value { get; }

        string? StringValue { get; }
    }
}
