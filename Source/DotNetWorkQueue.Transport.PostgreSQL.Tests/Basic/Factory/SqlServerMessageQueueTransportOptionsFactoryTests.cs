using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.Factory
{
    [TestClass]
    public class PostgreSqlMessageQueueTransportOptionsFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<PostgreSqlMessageQueueTransportOptionsFactory>();
            test.Create();
        }

        private static (PostgreSqlMessageQueueTransportOptionsFactory sut,
                        IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
                                      PostgreSqlMessageQueueTransportOptions> query)
            BuildWithConnectionString()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns("Host=localhost;Database=test");
            var query = Substitute.For<IQueryHandler<
                GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
                PostgreSqlMessageQueueTransportOptions>>();
            var sut = new PostgreSqlMessageQueueTransportOptionsFactory(connInfo, query);
            return (sut, query);
        }

        [TestMethod]
        public void Create_WhenStoreReturnsNull_DoesNotCacheTheDefaultFallback()
        {
            var (sut, query) = BuildWithConnectionString();
            query.Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>())
                 .Returns((PostgreSqlMessageQueueTransportOptions)null);

            var first = sut.Create();
            var second = sut.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            // Critical: handler invoked twice — default fallback was not cached
            query.Received(2).Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_WhenStoreReturnsOptions_CachesAndReturnsSameInstance()
        {
            var (sut, query) = BuildWithConnectionString();
            var stored = new PostgreSqlMessageQueueTransportOptions { EnableHistory = true };
            query.Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>())
                 .Returns(stored);

            var first = sut.Create();
            var second = sut.Create();

            Assert.AreSame(first, second);
            Assert.IsTrue(first.EnableHistory);
            // Happy path: exactly one load
            query.Received(1).Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_AfterDefaultFallback_ThenStoreHasOptions_ReturnsLoadedOptions()
        {
            var (sut, query) = BuildWithConnectionString();
            PostgreSqlMessageQueueTransportOptions stored = null;
            query.Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>())
                 .Returns(_ => stored);

            var firstDefaults = sut.Create();
            Assert.IsFalse(firstDefaults.EnableHistory);

            stored = new PostgreSqlMessageQueueTransportOptions { EnableHistory = true };
            var second = sut.Create();

            Assert.IsTrue(second.EnableHistory,
                "After queue creation the factory must observe the newly-persisted options.");
        }
    }
}

