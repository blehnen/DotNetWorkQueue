using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    public class QueueStatusProviderTests
    {
        [Fact]
        public void GetData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<ITransportOptionsFactory>();
            fixture.Inject(factory);
            var test = fixture.Create<QueueStatusProvider>();
            Assert.Equal(5, test.Current.Data.Count());
        }
    }
}
