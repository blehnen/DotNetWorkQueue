// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.Factory;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.Factory
{
    [TestClass]
    public class LiteDbMessageQueueTransportOptionsFactoryTests
    {
        private static (LiteDbMessageQueueTransportOptionsFactory sut,
                        IQueryHandler<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>,
                                      LiteDbMessageQueueTransportOptions> query,
                        IConnectionInformation connInfo)
            Build()
        {
            // Unique queue name + connection string to avoid InMemoryOptionsCache collisions across tests.
            var uniqueId = Guid.NewGuid().ToString("N");
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns("Filename=:memory:;Tag=" + uniqueId);
            connInfo.QueueName.Returns("q_" + uniqueId);
            var query = Substitute.For<IQueryHandler<
                GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>,
                LiteDbMessageQueueTransportOptions>>();
            var sut = new LiteDbMessageQueueTransportOptionsFactory(connInfo, query);
            return (sut, query, connInfo);
        }

        [TestMethod]
        public void Create_WhenStoreReturnsNullAndStaticCacheMisses_ReReadsStoreOnEachCall()
        {
            var (sut, query, _) = Build();
            query.Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>())
                 .Returns((LiteDbMessageQueueTransportOptions)null);

            var first = sut.Create();
            var second = sut.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            // The store is re-queried on every call until it returns non-null (or the
            // static InMemoryOptionsCache is populated), so a dashboard / cross-container
            // reader eventually observes the persisted options (GitHub issue #120).
            query.Received(2).Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_WhileStoreAndStaticCacheEmpty_ReturnsSameInstanceSoCallerMutationsPersist()
        {
            // Regression guard: the Creation class exposes `Options` via the factory.
            // Callers mutate that instance (e.g. `x.Options.EnableHistory = true`) and
            // expect the subsequent `CreateQueue` persist to see those mutations on
            // the SAME instance. Returning a fresh default on every call while the
            // store is empty silently drops those mutations and persists all-default
            // options — integration test regression observed on PR #137.
            var (sut, query, _) = Build();
            query.Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>())
                 .Returns((LiteDbMessageQueueTransportOptions)null);

            var first = sut.Create();
            first.EnableHistory = true;
            var second = sut.Create();

            Assert.AreSame(first, second,
                "While the store and static cache have no persisted options, Create() " +
                "must return the same tentative-default instance so caller mutations " +
                "survive across calls.");
            Assert.IsTrue(second.EnableHistory,
                "Mutations made to the first-returned instance must be visible on subsequent calls.");
        }

        [TestMethod]
        public void Create_WhenStoreReturnsOptions_CachesAndReturnsSameInstance()
        {
            var (sut, query, _) = Build();
            var stored = new LiteDbMessageQueueTransportOptions { EnableHistory = true };
            query.Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>())
                 .Returns(stored);

            var first = sut.Create();
            var second = sut.Create();

            Assert.AreSame(first, second);
            Assert.IsTrue(first.EnableHistory);
            query.Received(1).Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_AfterDefaultFallback_ThenStoreHasOptions_ReturnsLoadedOptions()
        {
            var (sut, query, _) = Build();
            LiteDbMessageQueueTransportOptions stored = null;
            query.Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>())
                 .Returns(_ => stored);

            var firstDefaults = sut.Create();
            Assert.IsFalse(firstDefaults.EnableHistory);

            stored = new LiteDbMessageQueueTransportOptions { EnableHistory = true };
            var second = sut.Create();

            Assert.IsTrue(second.EnableHistory,
                "After queue creation the factory must observe the newly-persisted options.");
        }

        [TestMethod]
        public void Create_WhenStoreReturnsNullAndStaticCacheHits_ReturnsCachedAndRemembersIt()
        {
            var (sut, query, connInfo) = Build();
            query.Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>())
                 .Returns((LiteDbMessageQueueTransportOptions)null);

            var cached = new LiteDbMessageQueueTransportOptions { EnableHistory = true };
            LiteDbMessageQueueTransportOptionsFactory.SaveToCache(connInfo, cached);

            var first = sut.Create();
            var second = sut.Create();

            Assert.AreSame(cached, first);
            Assert.AreSame(first, second);
            // InMemoryOptionsCache is real persisted data — cache hit DOES stick,
            // so the query handler is invoked exactly once (on the first Create).
            query.Received(1).Handle(Arg.Any<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>>());
        }
    }
}
