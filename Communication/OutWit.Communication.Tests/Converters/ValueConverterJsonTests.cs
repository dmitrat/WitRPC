using System;
using System.Reflection;
using Newtonsoft.Json;
using OutWit.Communication.Converters;
using OutWit.Communication.Tests.Mock.Interfaces;
using OutWit.Communication.Tests.Mock.Model;

namespace OutWit.Communication.Tests.Converters
{
    [TestFixture]
    public class ValueConverterJsonTests
    {
        [TestCase(1.23f)]
        [TestCase(-3.1415f)]
        public void FloatToDoubleConversionTest(float expected)
        {
            bool succeed = new ValueConverterJson().TryConvert(expected, typeof(double), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<double>());
            Assert.That(actual, Is.EqualTo((double)expected));
        }

        [TestCase(42)]
        public void Int32ToInt64ConversionTests(int expected)
        {
            bool succeed = new ValueConverterJson().TryConvert(expected, typeof(long), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<long>());
            Assert.That(actual, Is.EqualTo((long)expected));
        }

        [TestCase("2024-11-01")]
        public void SameTypeConversionTest(DateTime expected)
        {
            bool succeed = new ValueConverterJson().TryConvert(expected, typeof(DateTime), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<DateTime>());
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void JsonObjectToComplexTypeConversionTest()
        {
            var expected = new ComplexType { Int32Value = 42, StringValue = "Test" };

            object? jObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(expected));

            bool succeed = new ValueConverterJson().TryConvert(jObj, typeof(ComplexType), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<ComplexType>());
            Assert.That(((ComplexType)actual).Int32Value, Is.EqualTo(expected.Int32Value));
            Assert.That(((ComplexType)actual).StringValue, Is.EqualTo(expected.StringValue));
        }

        [Test]
        public void Int32ArrayConversionTest()
        {
            var expected = new[] { 1, 2, 3 };
            object? jObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(expected));

            bool succeed = new ValueConverterJson().TryConvert(jObj, typeof(int[]), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<int[]>());
            Assert.That(actual as int[], Is.EqualTo(expected));
        }

        [Test]
        public void Int32ListConversionTest()
        {
            var expected = new List<int> { 1, 2, 3 };
            object? jObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(expected));

            bool succeed = new ValueConverterJson().TryConvert(jObj, typeof(List<int>), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<List<int>>());
            Assert.That(actual as List<int>, Is.EqualTo(expected));
        }

        [Test]
        public void ComplexTypeArrayConversionTest()
        {
            var expected = new[]
            {
                new ComplexType { Int32Value = 1, StringValue = "One" },
                new ComplexType { Int32Value = 2, StringValue = "Two" }
            };
            object? jObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(expected));

            bool succeed = new ValueConverterJson().TryConvert(jObj, typeof(ComplexType[]), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<ComplexType[]>());
            var actualArray = actual as ComplexType[];
            Assert.That(actualArray!.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actualArray[i], Is.Not.Null);
                Assert.That(actualArray[i].Int32Value, Is.EqualTo(expected[i].Int32Value));
                Assert.That(actualArray[i].StringValue, Is.EqualTo(expected[i].StringValue));
            }
        }

        [Test]
        public void DerivedTypeToBaseTypeConversionTest()
        {
            bool succeed = new ValueConverterJson().TryConvert(new ComplexType(), typeof(IComplexType), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<ComplexType>());
        }

        [TestCase("FirstOption", EnumType.FirstOption)]
        [TestCase("SecondOption", EnumType.SecondOption)]
        public void StringToEnumConversionTest(string input, EnumType expected)
        {
            bool succeed = new ValueConverterJson().TryConvert(input, typeof(EnumType), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<EnumType>());
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0, EnumType.FirstOption)]
        [TestCase(1, EnumType.SecondOption)]
        public void Int32ToEnumConversionTest(int input, EnumType expected)
        {
            bool succeed = new ValueConverterJson().TryConvert(input, typeof(EnumType), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<EnumType>());
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void StringToGuidConversionTest()
        {
            var expected = Guid.NewGuid();
            bool succeed = new ValueConverterJson().TryConvert(expected.ToString(), typeof(Guid), out object? actual);

            Assert.That(succeed, Is.True);
            Assert.That(actual, Is.TypeOf<Guid>());
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("Empty")]
        [TestCase("NewGuid")]
        public void RoundTripGuidConversionTest(string valueData)
        {
            PerformRoundTripTest(ParseTestData<Guid>(valueData));
        }

        [TestCase("Zero")]
        [TestCase("MinValue")]
        [TestCase("MaxValue")]
        [TestCase("-00:00:05.9167374")]
        public void RoundTripTimeSpanConversionTest(string valueData)
        {
            PerformRoundTripTest(ParseTestData<TimeSpan>(valueData));
        }

        [TestCase("Now")]
        [TestCase("Today")]
        [TestCase("MinValue")]
        [TestCase("MaxValue")]
        [TestCase("2020-02-05 3:10:27 PM")]
        public void RoundTripDateTimeConversionTest(string valueData)
        {
            PerformRoundTripTest(ParseTestData<DateTime>(valueData),
                assertAreEqual: (x, y) => Assert.That(
                    DateTime.SpecifyKind(x, DateTimeKind.Unspecified),
                    Is.EqualTo(DateTime.SpecifyKind(y, DateTimeKind.Unspecified))));
        }



        private T ParseTestData<T>(string valueData)
        {
            if (typeof(T).GetMember(valueData).FirstOrDefault() is MemberInfo member)
            {
                if (member is FieldInfo field)
                    return (T)field.GetValue(null);
                else if (member is PropertyInfo property)
                    return (T)property.GetValue(null);
                else if (member is MethodInfo method)
                    return (T)method.Invoke(null, null);
            }

            var parseMethod = typeof(T).GetMethod("Parse", new[] { typeof(string) });

            return (T)parseMethod.Invoke(null, new[] { valueData });
        }

        private void PerformRoundTripTest<T>(T value, Action<T, T>? assertAreEqual = null)
        {
            bool succeed = new ValueConverterJson().TryConvert(value, typeof(string), out object? intermediate);
            bool succeed2 = new ValueConverterJson().TryConvert(intermediate, typeof(T), out object? final);

            Assert.That(succeed, Is.True);
            Assert.That(succeed2, Is.True);
            Assert.That(final, Is.TypeOf<T>());

            if (assertAreEqual != null)
                assertAreEqual(value, (T)final);
            else
                Assert.That(final, Is.EqualTo(value));
        }
    }


}
