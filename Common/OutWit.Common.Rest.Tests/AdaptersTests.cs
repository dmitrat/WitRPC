using System;
using OutWit.Common.Rest.Tests.Model;
using OutWit.Common.Rest.Utils;

namespace OutWit.Common.Rest.Tests
{
    [TestFixture]
    public class AdaptersTests
    {
        [Test]
        public void ConvertTest()
        {
            Assert.That(((byte)1).Convert(), Is.EqualTo("1"));
            Assert.That(((sbyte)2).Convert(), Is.EqualTo("2"));
            Assert.That(((short)-3).Convert(), Is.EqualTo("-3"));
            Assert.That(((ushort)4).Convert(), Is.EqualTo("4"));
            Assert.That(((int)-5).Convert(), Is.EqualTo("-5"));
            Assert.That(((uint)6).Convert(), Is.EqualTo("6"));
            Assert.That(((long)-7).Convert(), Is.EqualTo("-7"));
            Assert.That(((ulong)8).Convert(), Is.EqualTo("8"));
            Assert.That(('F').Convert(), Is.EqualTo("F"));
            Assert.That(("Text").Convert(), Is.EqualTo("Text"));
            Assert.That((true).Convert(), Is.EqualTo("true"));
            Assert.That(((float)10.1112).Convert(), Is.EqualTo("10.1112"));
            Assert.That(((double)13.1415).Convert(), Is.EqualTo("13.1415"));
            Assert.That(Guid.Parse("43267E86-90A0-4CEA-B9B7-C8F9085DD4AE").Convert(), 
                Is.EqualTo("43267E86-90A0-4CEA-B9B7-C8F9085DD4AE"));

            Assert.That((new TimeSpan(1, 2, 3, 4)).Convert(), Is.EqualTo("1.02:03:04"));
            Assert.That((new DateOnly(2021, 2, 3)).Convert(), Is.EqualTo($"02/03/2021"));
            Assert.That((new TimeOnly(1, 2, 3)).Convert(), Is.EqualTo($"01:02:03"));
            Assert.That((new DateTime(2021, 2, 3, 4, 5, 6)).Convert(), Is.EqualTo("02/03/2021 04:05:06"));
            Assert.That((new DateTimeOffset(2021, 2, 3, 4, 5, 6, new TimeSpan())).Convert(), Is.EqualTo("02/03/2021 04:05:06"));
            Assert.That((TestEnum.Option2).Convert(), Is.EqualTo("Option2"));
        }

        [Test]
        public void CanAppendTest()
        {
            Assert.That(((byte)1).CanAppend(), Is.True);
            Assert.That(((sbyte)2).CanAppend(), Is.True);
            Assert.That(((short)-3).CanAppend(), Is.True);
            Assert.That(((ushort)4).CanAppend(), Is.True);
            Assert.That(((int)-5).CanAppend(), Is.True);
            Assert.That(((uint)6).CanAppend(), Is.True);
            Assert.That(((long)-7).CanAppend(), Is.True);
            Assert.That(((ulong)8).CanAppend(), Is.True);
            Assert.That(('F').CanAppend(), Is.True);
            Assert.That(("Text").CanAppend(), Is.True);
            Assert.That((true).CanAppend(), Is.True);
            Assert.That(((float)10.1112).CanAppend(), Is.True);
            Assert.That(((double)13.1415).CanAppend(), Is.True);
            Assert.That(Guid.Parse("43267E86-90A0-4CEA-B9B7-C8F9085DD4AE").CanAppend(), Is.True);
            Assert.That((new TimeSpan(1, 2, 3, 4)).CanAppend(), Is.True);
            Assert.That((new DateOnly(2021, 2, 3)).CanAppend(), Is.True);
            Assert.That((new TimeOnly(1, 2, 3)).CanAppend(), Is.True);
            Assert.That((new DateTime(2021, 2, 3, 4, 5, 6)).CanAppend(), Is.True);
            Assert.That((new DateTimeOffset(2021, 2, 3, 4, 5, 6, new TimeSpan())).CanAppend(), Is.True);
            Assert.That((TestEnum.Option2).CanAppend(), Is.True);


            Assert.That((typeof(byte)).CanAppend(), Is.True);
            Assert.That((typeof(sbyte)).CanAppend(), Is.True);
            Assert.That((typeof(short)).CanAppend(), Is.True);
            Assert.That((typeof(ushort)).CanAppend(), Is.True);
            Assert.That((typeof(int)).CanAppend(), Is.True);
            Assert.That((typeof(uint)).CanAppend(), Is.True);
            Assert.That((typeof(long)).CanAppend(), Is.True);
            Assert.That((typeof(ulong)).CanAppend(), Is.True);
            Assert.That((typeof(char)).CanAppend(), Is.True);
            Assert.That((typeof(string)).CanAppend(), Is.True);
            Assert.That((typeof(bool)).CanAppend(), Is.True);
            Assert.That((typeof(float)).CanAppend(), Is.True);
            Assert.That((typeof(double)).CanAppend(), Is.True);
            Assert.That(typeof(Guid).CanAppend(), Is.True);
            Assert.That((typeof(TimeSpan)).CanAppend(), Is.True);
            Assert.That((typeof(DateOnly)).CanAppend(), Is.True);
            Assert.That((typeof(TimeOnly)).CanAppend(), Is.True);
            Assert.That((typeof(DateTime)).CanAppend(), Is.True);
            Assert.That((typeof(DateTimeOffset)).CanAppend(), Is.True);
            Assert.That((typeof(TestEnum)).CanAppend(), Is.True);
        }
    }
}
