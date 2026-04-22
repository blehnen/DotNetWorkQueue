using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.Factory
{
    [TestClass]
    public class SqlServerMessageQueueTransportOptionsFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<SqlServerMessageQueueTransportOptionsFactory>();
            test.Create();
        }

        private static (SqlServerMessageQueueTransportOptionsFactory sut,
                        IQueryHandler<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>,
                                      SqlServerMessageQueueTransportOptions> query)
            BuildWithConnectionString()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns("Server=localhost;Database=test");
            var query = Substitute.For<IQueryHandler<
                GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>,
                SqlServerMessageQueueTransportOptions>>();
            var sut = new SqlServerMessageQueueTransportOptionsFactory(connInfo, query);
            return (sut, query);
        }

        [TestMethod]
        public void Create_WhenStoreReturnsNull_DoesNotCacheTheDefaultFallback()
        {
            var (sut, query) = BuildWithConnectionString();
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>>())
                 .Returns((SqlServerMessageQueueTransportOptions)null);

            var first = sut.Create();
            var second = sut.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            query.Received(2).Handle(Arg.Any<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_WhenStoreReturnsOptions_CachesAndReturnsSameInstance()
        {
            var (sut, query) = BuildWithConnectionString();
            var stored = new SqlServerMessageQueueTransportOptions { EnableHistory = true };
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>>())
                 .Returns(stored);

            var first = sut.Create();
            var second = sut.Create();

            Assert.AreSame(first, second);
            Assert.IsTrue(first.EnableHistory);
            query.Received(1).Handle(Arg.Any<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_AfterDefaultFallback_ThenStoreHasOptions_ReturnsLoadedOptions()
        {
            var (sut, query) = BuildWithConnectionString();
            SqlServerMessageQueueTransportOptions stored = null;
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqlServerMessageQueueTransportOptions>>())
                 .Returns(_ => stored);

            var firstDefaults = sut.Create();
            Assert.IsFalse(firstDefaults.EnableHistory);

            stored = new SqlServerMessageQueueTransportOptions { EnableHistory = true };
            var second = sut.Create();

            Assert.IsTrue(second.EnableHistory,
                "After queue creation the factory must observe the newly-persisted options.");
        }
    }
}
