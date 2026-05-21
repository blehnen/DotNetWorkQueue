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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    /// <summary>
    /// SimpleInjector container smoke tests proving the factory-delegate registration in
    /// <see cref="PostgreSqlMessageQueueInit"/> branches on
    /// <see cref="PostgreSqlMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted"/>
    /// per PROJECT.md §Functional New Public API and §Success Criteria #2.
    /// </summary>
    [TestClass]
    public class PostgreSqlRelationalWorkerNotificationRegistrationTests
    {
        private const string FakeConnection =
            "Host=localhost;Username=postgres;Password=password;Database=test";

        [TestMethod]
        public void Resolves_Relational_When_HoldTransaction_Enabled()
        {
            IWorkerNotification notification = null;
            var stubOptions = new PostgreSqlMessageQueueTransportOptions
            {
                EnableHoldTransactionUntilMessageCommitted = true
            };

            using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>(
                registerService: container =>
                {
                    var stubFactory = Substitute.For<ITransportOptionsFactory>();
                    stubFactory.Create().Returns(stubOptions);
                    container.Register<ITransportOptionsFactory>(() => stubFactory, LifeStyles.Singleton);
                },
                setOptions: container =>
                {
                    notification = container.GetInstance<IWorkerNotification>();
                }))
            {
                try { qc.CreateConsumer(new QueueConnection("ADMIN", FakeConnection)); }
                catch { /* downstream resolution may throw on the fake connection — smoke only needs the IWorkerNotification cast above */ }
            }

            Assert.IsNotNull(notification, "Container setOptions callback should have resolved IWorkerNotification.");
            Assert.IsTrue(notification is IRelationalWorkerNotification,
                "With EnableHoldTransactionUntilMessageCommitted=true, container must resolve PostgreSqlRelationalWorkerNotification.");
        }

        [TestMethod]
        public void Resolves_NonRelational_When_HoldTransaction_Disabled()
        {
            IWorkerNotification notification = null;
            var stubOptions = new PostgreSqlMessageQueueTransportOptions
            {
                EnableHoldTransactionUntilMessageCommitted = false
            };

            using (var qc = new QueueContainer<PostgreSqlMessageQueueInit>(
                registerService: container =>
                {
                    var stubFactory = Substitute.For<ITransportOptionsFactory>();
                    stubFactory.Create().Returns(stubOptions);
                    container.Register<ITransportOptionsFactory>(() => stubFactory, LifeStyles.Singleton);
                },
                setOptions: container =>
                {
                    notification = container.GetInstance<IWorkerNotification>();
                }))
            {
                try { qc.CreateConsumer(new QueueConnection("ADMIN", FakeConnection)); }
                catch { /* downstream resolution may throw on the fake connection — smoke only needs the IWorkerNotification cast above */ }
            }

            Assert.IsNotNull(notification, "Container setOptions callback should have resolved IWorkerNotification.");
            Assert.IsFalse(notification is IRelationalWorkerNotification,
                "With EnableHoldTransactionUntilMessageCommitted=false, container must resolve the plain WorkerNotification so the capability cast cleanly fails (PROJECT.md §Success Criteria #2).");
        }
    }
}
