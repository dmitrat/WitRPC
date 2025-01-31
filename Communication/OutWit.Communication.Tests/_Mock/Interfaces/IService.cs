using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Communication.Tests._Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Mock.Interfaces
{
    public interface IService: IServiceBase
    {
        public T GenericSimple<T>(int number, string text, T value);

        public Task<T> GenericSimpleAsync<T>(int number, string text, T value);


        public ComplexNumber<T1, T2> GenericComplex<T1, T2>(int number, string text, ComplexNumber<T1, T2> value);

        public Task<ComplexNumber<T1, T2>> GenericComplexAsync<T1, T2>(int number, string text, ComplexNumber<T1, T2> value);


        public ComplexNumber<T1, T2> GenericComplexArray<T1, T2>(int number, string text, List<ComplexNumber<T1, T2>> value);

        public Task<ComplexNumber<T1, T2>> GenericComplexArrayAsync<T1, T2>(int number, string text, List<ComplexNumber<T1, T2>> value);


        public ComplexNumber<T2, T3> GenericComplexMulti<T1, T2, T3, T4>(ComplexNumber<T1, T2> number, string text, List<ComplexNumber<T3, T4>> value);

        public Task<ComplexNumber<T2, T3>> GenericComplexMultiAsync<T1, T2, T3, T4>(ComplexNumber<T1, T2> number, string text, List<ComplexNumber<T3, T4>> value);

    }






}
