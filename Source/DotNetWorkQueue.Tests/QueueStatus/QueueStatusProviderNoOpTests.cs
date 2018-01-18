using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.QueueStatus;
using NSubstitute;



using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueStatusProviderNoOpTests
    {
        [Theory, AutoData]
        public void Create_Default(string name, string connection, string path)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var conn = fixture.Create<IConnectionInformation>();
            conn.QueueName.Returns(name);
            conn.ConnectionString.Returns(connection);
            fixture.Inject(conn);
            var test = fixture.Create<QueueStatusProviderNoOp>();
            Assert.NotNull(test.Current);
            Assert.Null(test.Error);
            Assert.Equal(name, test.Name);
            Assert.Null(test.HandlePath(path));
            Assert.Equal(string.Empty, test.Server);
        }
    }
}
