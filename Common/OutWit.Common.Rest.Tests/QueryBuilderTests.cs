using OutWit.Common.Rest.Tests.Model;

namespace OutWit.Common.Rest.Tests
{
    [TestFixture]
    public class QueryBuilderTests
    {
        [Test]
        public async Task QueryStringTest()
        {
            var builder = new QueryBuilder();

            builder.AddParameter("1", "1");
            builder.AddParameter("2", 2);
            builder.AddParameter("3", 3.33333);
            builder.AddParameter("4", "5", "6", "7");
            builder.AddParameter("8", TestEnum.Option1);
            builder.AddParameter("9", new DateTime(2021, 2, 3, 4, 5, 6));

            var query = await builder.AsStringAsync();
            Console.WriteLine(query);

            Assert.That(query, Is.EqualTo("1=1&2=2&3=3.33333&4=5%2C6%2C7&8=Option1&9=02%2F03%2F2021+04%3A05%3A06"));

        }

    }
}