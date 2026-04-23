using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic.Factory
{
    [TestClass]
    public class SqLiteMessageQueueTransportOptionsFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<SqLiteMessageQueueTransportOptionsFactory>();
            test.Create();
        }

        private static (SqLiteMessageQueueTransportOptionsFactory sut,
                        IQueryHandler<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>,
                                      SqLiteMessageQueueTransportOptions> query)
            BuildWithConnectionString()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns("Data Source=:memory:");
            var query = Substitute.For<IQueryHandler<
                GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>,
                SqLiteMessageQueueTransportOptions>>();
            var sut = new SqLiteMessageQueueTransportOptionsFactory(connInfo, query);
            return (sut, query);
        }

        [TestMethod]
        public void Create_WhenStoreReturnsNull_ReReadsStoreOnEachCall()
        {
            var (sut, query) = BuildWithConnectionString();
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>>())
                 .Returns((SqLiteMessageQueueTransportOptions)null);

            var first = sut.Create();
            var second = sut.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            // The store is re-queried on every call until it returns non-null,
            // so a dashboard / cross-container reader eventually observes the
            // persisted options (GitHub issue #120).
            query.Received(2).Handle(Arg.Any<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_WhileStoreEmpty_ReturnsSameInstanceSoCallerMutationsPersist()
        {
            // Regression guard: the Creation class exposes `Options` via the factory.
            // Callers mutate that instance (e.g. `x.Options.EnableHistory = true`) and
            // expect the subsequent `CreateQueue` persist to see those mutations on
            // the SAME instance. Returning a fresh default on every call while the
            // store is empty silently drops those mutations and persists all-default
            // options — integration test regression observed on PR #137.
            var (sut, query) = BuildWithConnectionString();
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>>())
                 .Returns((SqLiteMessageQueueTransportOptions)null);

            var first = sut.Create();
            first.EnableHistory = true;
            var second = sut.Create();

            Assert.AreSame(first, second,
                "While the store has no persisted options, Create() must return the " +
                "same tentative-default instance so caller mutations survive across calls.");
            Assert.IsTrue(second.EnableHistory,
                "Mutations made to the first-returned instance must be visible on subsequent calls.");
        }

        [TestMethod]
        public void Create_WhenStoreReturnsOptions_CachesAndReturnsSameInstance()
        {
            var (sut, query) = BuildWithConnectionString();
            var stored = new SqLiteMessageQueueTransportOptions { EnableHistory = true };
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>>())
                 .Returns(stored);

            var first = sut.Create();
            var second = sut.Create();

            Assert.AreSame(first, second);
            Assert.IsTrue(first.EnableHistory);
            query.Received(1).Handle(Arg.Any<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_AfterDefaultFallback_ThenStoreHasOptions_ReturnsLoadedOptions()
        {
            var (sut, query) = BuildWithConnectionString();
            SqLiteMessageQueueTransportOptions stored = null;
            query.Handle(Arg.Any<GetQueueOptionsQuery<SqLiteMessageQueueTransportOptions>>())
                 .Returns(_ => stored);

            var firstDefaults = sut.Create();
            Assert.IsFalse(firstDefaults.EnableHistory);

            stored = new SqLiteMessageQueueTransportOptions { EnableHistory = true };
            var second = sut.Create();

            Assert.IsTrue(second.EnableHistory,
                "After queue creation the factory must observe the newly-persisted options.");
        }
    }
}
