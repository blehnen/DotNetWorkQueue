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
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    /// <summary>
    /// SimpleInjector container smoke tests proving the factory-delegate registration in
    /// <see cref="SQLServerMessageQueueInit"/> branches on
    /// <see cref="SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted"/>
    /// per PROJECT.md §Functional New Public API and §Success Criteria #2.
    /// </summary>
    [TestClass]
    public class SqlServerRelationalWorkerNotificationRegistrationTests
    {
        private const string FakeConnection =
            "Server=localhost;Application Name=Test;Database=Test;User ID=sa;Password=password";

        [TestMethod]
        public void Resolves_Relational_When_HoldTransaction_Enabled()
        {
            IWorkerNotification notification = null;
            var stubOptions = new SqlServerMessageQueueTransportOptions
            {
                EnableHoldTransactionUntilMessageCommitted = true
            };

            using (var qc = new QueueContainer<SqlServerMessageQueueInit>(
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
                "With EnableHoldTransactionUntilMessageCommitted=true, container must resolve SqlServerRelationalWorkerNotification.");
        }

        [TestMethod]
        public void Resolves_NonRelational_When_HoldTransaction_Disabled()
        {
            IWorkerNotification notification = null;
            var stubOptions = new SqlServerMessageQueueTransportOptions
            {
                EnableHoldTransactionUntilMessageCommitted = false
            };

            using (var qc = new QueueContainer<SqlServerMessageQueueInit>(
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
