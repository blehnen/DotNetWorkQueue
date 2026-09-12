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
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.History.Implementation
{
    /// <summary>
    /// A history timestamp comes from the time provider the queue was configured with.
    ///
    /// The unit tests construct the history handlers directly, so they say nothing about whether the
    /// container hands the handler the provider the user chose. This registers a clock of its own the
    /// ordinary way - the same callback an application would use - resolves the handler the queue would
    /// resolve, and reads the timestamp back out through the query the dashboard uses. It therefore
    /// covers the wiring and the round trip to storage, per transport.
    /// </summary>
    public class HistoryTimeProviderTest
    {
        /// <summary>A date far from any machine clock, so a timestamp from the wrong source cannot match.</summary>
        public static readonly DateTime FixedUtc = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

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

                    using (var container = Container<TTransportInit>(logProvider, scope))
                    using (var admin = container.CreateAdminContainer(queueConnection))
                    {
                        var history = admin.GetInstance<IWriteMessageHistory>();
                        history.RecordEnqueue("1", "c1", "routeA", "MyType", new byte[] { 1 }, new byte[] { 2 });
                    }

                    //a fresh container, so the record is read back from storage rather than from anything
                    //the writing container may still be holding
                    using (var container = Container<TTransportInit>(logProvider, scope))
                    using (var admin = container.CreateAdminContainer(queueConnection))
                    {
                        var records = admin.GetInstance<IQueryMessageHistory>().Get(0, 10, null);
                        Assert.IsGreaterThanOrEqualTo(1, records.Count, "the history record was not written");

                        //what is being asked is where the timestamp came from, so the comparison is
                        //against the clock rather than against the machine's. It is deliberately not an
                        //exact match: PostgreSQL and SQLite return the value shifted by the machine's
                        //UTC offset, which is GitHub #311 and not this test's subject. A day of slack
                        //covers any offset while leaving twenty-five years between the two candidates.
                        var drift = (records[0].EnqueuedUtc - FixedUtc).Duration();
                        Assert.IsLessThanOrEqualTo(TimeSpan.FromDays(1), drift,
                            $"expected a timestamp from the configured provider (near {FixedUtc:u}), got {records[0].EnqueuedUtc:u}");
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

        private static QueueContainer<TTransportInit> Container<TTransportInit>(
            Microsoft.Extensions.Logging.ILogger logProvider, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            return new QueueContainer<TTransportInit>(serviceRegister =>
            {
                serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                serviceRegister.RegisterNonScopedSingleton(scope);
                //IGetTimeFactory, not IGetTime: Redis registers a factory of its own that answers with
                //the Redis server's clock and never consults an IGetTime registration, and the factory
                //is what the handlers actually ask
                serviceRegister.Register<IGetTimeFactory, FixedTimeFactory>(LifeStyles.Singleton);
            });
        }

        /// <summary>A clock that does not move, and is nowhere near the machine's.</summary>
        private class FixedTimeFactory : IGetTimeFactory
        {
            public IGetTime Create() => new FixedTime();
        }

        private class FixedTime : IGetTime
        {
            public DateTime GetCurrentUtcDate() => FixedUtc;
            public TimeSpan GetCurrentOffset => TimeSpan.Zero;
            public string Name => "Fixed";
        }
    }
}
