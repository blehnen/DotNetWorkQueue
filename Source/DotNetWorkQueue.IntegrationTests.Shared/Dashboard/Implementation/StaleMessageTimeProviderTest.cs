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
using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.Dashboard.Implementation
{
    /// <summary>
    /// The dashboard's stale-message cut-off comes from the time provider the queue was configured
    /// with, not from the machine clock.
    ///
    /// A fixed clock in the past cannot show this. The cut-off is "now minus the threshold", and under
    /// either clock a heartbeat written moments ago is newer than a cut-off derived from a past date,
    /// so both sources answer "nothing is stale" and the test passes on the bug. The clock here is
    /// therefore fixed in the future, which makes the two sources disagree:
    ///
    /// - configured clock: cut-off is <see cref="FixedFutureUtc"/> minus the threshold, still years
    ///   after the heartbeat, so the message IS stale and comes back
    /// - machine clock: cut-off is a couple of minutes ago, the heartbeat is newer than that, so
    ///   nothing comes back
    ///
    /// Both assertions are needed. The first fails if a handler goes back to <c>DateTime.UtcNow</c>;
    /// the second is what rules out a query that simply returns everything it finds.
    ///
    /// Not every transport belongs here. SQL Server stores HeartBeat as a <c>datetime</c> written
    /// server-side by <c>GETUTCDATE()</c> and compares it against <c>GETUTCDATE()</c>, so the database
    /// clock is both sides of the comparison and the configured provider is deliberately absent from
    /// each - it would fail this scenario while being entirely self-consistent. PostgreSQL and SQLite
    /// store ticks, which the client has to supply, so there the provider is what both sides use.
    /// </summary>
    public class StaleMessageTimeProviderTest
    {
        /// <summary>
        /// The clock the queue is configured with: fixed for the run, and far enough ahead of the
        /// machine that a heartbeat written during the test is unambiguously older than a cut-off
        /// derived from it. Taken relative to the machine rather than written as a literal date, so
        /// there is no year in which this scenario quietly stops discriminating.
        /// </summary>
        public static readonly DateTime FixedFutureUtc = DateTime.UtcNow.AddYears(20);

        private const int ThresholdSeconds = 120;

        public void Run<TTransportInit, TTransportCreate>(
            QueueConnection queueConnection,
            Action<TTransportCreate> setOptions)
            where TTransportInit : ITransportInit, new()
            where TTransportCreate : class, IQueueCreation
        {
            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator = new QueueCreationContainer<TTransportInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var reachedHandler = new ManualResetEventSlim(false);
                    var releaseHandler = new ManualResetEventSlim(false);
                    var handlerFinished = new ManualResetEventSlim(false);

                    //the producer and consumer run on the ordinary clock, so the heartbeat lands at
                    //the real current time - it is the dashboard query below that gets the fixed one
                    using (var creator = Container<TTransportInit>(logProvider, scope, fixedClock: false))
                    {
                        using (var producer = creator.CreateProducer<FakeMessage>(queueConnection))
                        {
                            producer.Send(new FakeMessage());
                        }

                        using (var consumer = creator.CreateConsumer(queueConnection))
                        {
                            //long enough that no beat lands while the assertions run, and still inside
                            //the consumer configuration guard (Time must be at least 3x UpdateTime)
                            SharedSetup.SetupDefaultConsumerQueue(consumer.Configuration, 1,
                                TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10), null);

                            consumer.Start<FakeMessage>((message, notifications) =>
                            {
                                reachedHandler.Set();
                                //Hold the message in Processing until the assertions are done with it.
                                //The timeout is a safety net against a hung run, not a budget for the
                                //assertions: if it ever fires the row leaves Processing, and the counts
                                //below would then be zero for a reason that has nothing to do with the
                                //clock. handlerFinished is what keeps that from being read as a clock
                                //failure.
                                releaseHandler.Wait(TimeSpan.FromMinutes(10));
                                handlerFinished.Set();
                            }, CreateNotifications.Create(logProvider));

                            try
                            {
                                Assert.IsTrue(reachedHandler.Wait(TimeSpan.FromSeconds(60)),
                                    "the message never reached a handler, so it was never in a processing state to be stale");

                                //the heartbeat was written a moment ago on the machine clock
                                var machineClockCount = StaleCount<TTransportInit>(logProvider, scope, queueConnection, fixedClock: false);
                                var configuredClockCount = StaleCount<TTransportInit>(logProvider, scope, queueConnection, fixedClock: true);

                                //neither count means anything if the message stopped being processed
                                //while they were taken
                                Assert.IsFalse(handlerFinished.IsSet,
                                    "the handler released the message before the stale queries had run, so the counts say nothing about which clock was used");

                                Assert.AreEqual(0, machineClockCount,
                                    "a message whose heartbeat is seconds old was reported as stale against a two minute threshold");

                                Assert.AreEqual(1, configuredClockCount,
                                    $"the cut-off did not come from the configured clock ({FixedFutureUtc:O}); a message held in processing should be stale against it");
                            }
                            finally
                            {
                                releaseHandler.Set();
                            }
                        }
                    }
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }
            }
        }

        /// <summary>
        /// How many messages the dashboard considers stale, asked through a container using either the
        /// machine clock or the fixed one.
        /// </summary>
        private static int StaleCount<TTransportInit>(Microsoft.Extensions.Logging.ILogger logProvider,
            ICreationScope scope, QueueConnection queueConnection, bool fixedClock)
            where TTransportInit : ITransportInit, new()
        {
            using (var container = Container<TTransportInit>(logProvider, scope, fixedClock))
            using (var admin = container.CreateAdminContainer(queueConnection))
            {
                var handler = admin
                    .GetInstance<IQueryHandlerAsync<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>>();
                return handler.HandleAsync(new GetDashboardStaleMessagesQuery(ThresholdSeconds, 0, 100))
                    .GetAwaiter().GetResult().Count;
            }
        }

        private static QueueContainer<TTransportInit> Container<TTransportInit>(
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope, bool fixedClock)
            where TTransportInit : ITransportInit, new()
        {
            return new QueueContainer<TTransportInit>(serviceRegister =>
            {
                serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                serviceRegister.RegisterNonScopedSingleton(scope);
                if (fixedClock)
                {
                    //IGetTimeFactory, not IGetTime: Redis registers a factory of its own that answers
                    //with the Redis server's clock and never consults an IGetTime registration, and the
                    //factory is what the handlers actually ask
                    serviceRegister.Register<IGetTimeFactory, FixedTimeFactory>(LifeStyles.Singleton);
                }
            });
        }

        /// <summary>A clock that does not move, and sits far enough ahead that anything written on the
        /// machine clock is old against it.</summary>
        private class FixedTimeFactory : IGetTimeFactory
        {
            public IGetTime Create() => new FixedTime();
        }

        private class FixedTime : IGetTime
        {
            public DateTime GetCurrentUtcDate() => FixedFutureUtc;
            public TimeSpan GetCurrentOffset => TimeSpan.Zero;
            public string Name => "FixedFuture";
        }
    }
}
