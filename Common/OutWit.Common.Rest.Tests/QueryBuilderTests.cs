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

            var query = await builder.AsStringAsync();

            Assert.That(query, Is.EqualTo("1=1&2=2&3=3.333330000&4=5%2C6%2C7"));

        }

    }
}