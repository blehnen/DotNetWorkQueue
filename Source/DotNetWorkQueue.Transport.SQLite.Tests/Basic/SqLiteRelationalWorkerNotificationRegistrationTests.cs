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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// SimpleInjector container smoke tests proving the factory-delegate registration in
    /// <see cref="SqLiteMessageQueueSharedInit"/> branches on
    /// <see cref="SqLiteMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted"/>
    /// per PROJECT.md §Functional New Public API and §Success Criteria #2.
    /// </summary>
    [TestClass]
    public class SqLiteRelationalWorkerNotificationRegistrationTests
    {
        private const string FakeConnection = "Data Source=:memory:";

        private sealed class StubOptionsFactory : ITransportOptionsFactory
        {
            private readonly ITransportOptions _options;
            public StubOptionsFactory(ITransportOptions options) { _options = options; }
            public ITransportOptions Create() => _options;
        }

        [TestMethod]
        public void Resolves_Relational_When_HoldTransaction_Enabled()
        {
            IWorkerNotification notification = null;
            var stubOptions = new SqLiteMessageQueueTransportOptions
            {
                EnableHoldTransactionUntilMessageCommitted = true
            };

            using (var qc = new QueueContainer<SqLiteMessageQueueInit>(
                registerService: container =>
                {
                    container.Register<ITransportOptionsFactory>(() => new StubOptionsFactory(stubOptions), LifeStyles.Singleton);
                },
                setOptions: container =>
                {
                    notification = container.GetInstance<IWorkerNotification>();
                }))
            {
                try { qc.CreateConsumer(new QueueConnection("ADMIN", FakeConnection)); }
                catch { /* downstream resolution may throw; smoke only needs the IWorkerNotification cast above */ }
            }

            Assert.IsNotNull(notification);
            Assert.IsTrue(notification is IRelationalWorkerNotification,
                "With EnableHoldTransactionUntilMessageCommitted=true, container must resolve SqLiteRelationalWorkerNotification.");
        }

        [TestMethod]
        public void Resolves_NonRelational_When_HoldTransaction_Disabled()
        {
            IWorkerNotification notification = null;
            var stubOptions = new SqLiteMessageQueueTransportOptions
            {
                EnableHoldTransactionUntilMessageCommitted = false
            };

            using (var qc = new QueueContainer<SqLiteMessageQueueInit>(
                registerService: container =>
                {
                    container.Register<ITransportOptionsFactory>(() => new StubOptionsFactory(stubOptions), LifeStyles.Singleton);
                },
                setOptions: container =>
                {
                    notification = container.GetInstance<IWorkerNotification>();
                }))
            {
                try { qc.CreateConsumer(new QueueConnection("ADMIN", FakeConnection)); }
                catch { /* downstream resolution may throw; smoke only needs the cast above */ }
            }

            Assert.IsNotNull(notification);
            Assert.IsFalse(notification is IRelationalWorkerNotification,
                "With EnableHoldTransactionUntilMessageCommitted=false, container must resolve plain WorkerNotification so the capability cast cleanly fails (PROJECT.md §Success Criteria #2).");
        }
    }
}
