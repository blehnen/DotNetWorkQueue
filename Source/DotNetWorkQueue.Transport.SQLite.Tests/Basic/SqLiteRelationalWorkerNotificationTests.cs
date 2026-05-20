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
using System.Data;
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Contract + behavior tests for <see cref="SqLiteRelationalWorkerNotification"/>.
    /// Pairs with <see cref="SqLiteRelationalWorkerNotificationRegistrationTests"/> for
    /// the option-driven SimpleInjector smoke tests.
    /// </summary>
    [TestClass]
    public class SqLiteRelationalWorkerNotificationTests
    {
        // SqLiteRelationalWorkerNotification stores state in a static AsyncLocal so the
        // receive path can inject state visible to a different transient instance the
        // user handler observes. Clear it before AND after each test so the order of
        // test execution does not leak state between [TestMethod]s on the same thread.
        [TestInitialize]
        public void ClearAsyncLocalBefore() => SqLiteRelationalWorkerNotification.ClearCurrent();

        [TestCleanup]
        public void ClearAsyncLocalAfter() => SqLiteRelationalWorkerNotification.ClearCurrent();

        private static SqLiteRelationalWorkerNotification CreateSubject()
        {
            var configuration = new TransportConfigurationReceive(
                Substitute.For<IConnectionInformation>(),
                Substitute.For<IQueueDelayFactory>(),
                Substitute.For<IRetryDelayFactory>())
            { MessageRollbackSupported = true };
            return new SqLiteRelationalWorkerNotification(
                Substitute.For<IHeaders>(),
                Substitute.For<IQueueCancelWork>(),
                configuration,
                Substitute.For<ILogger>(),
                Substitute.For<IMetrics>(),
                new ActivitySource("test"));
        }

        [TestMethod]
        public void Constructor_Passes_Args_To_Base()
        {
            var subject = CreateSubject();

            Assert.IsNotNull(subject.WorkerStopping);
            Assert.IsNotNull(subject.HeaderNames);
            Assert.IsNotNull(subject.Log);
            Assert.IsNotNull(subject.Metrics);
            Assert.IsNotNull(subject.Tracer);
            Assert.IsTrue(subject.TransportSupportsRollback);
        }

        [TestMethod]
        public void Transaction_Returns_Null_When_ConnectionState_Not_Set()
        {
            var subject = CreateSubject();

            Assert.IsNull(subject.Transaction);
        }

        [TestMethod]
        public void ConnectionState_PropertySet_Round_Trips()
        {
            var subject = CreateSubject();
            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var state = new SqLiteConnectionState(connection, transaction);

            subject.ConnectionState = state;

            Assert.IsNotNull(subject.ConnectionState);
            Assert.AreSame(state, subject.ConnectionState);
        }

        [TestMethod]
        public void Transaction_Returns_Null_When_State_Transaction_Not_DbTransaction()
        {
            // NSubstitute IDbTransaction substitute is not a DbTransaction-derived runtime
            // type, so the `as DbTransaction` cast returns null. Documents the delegation
            // path executes without throwing when state is set. Phase 7 integration tests
            // cover the non-null-return path with a real Microsoft.Data.Sqlite SqliteTransaction.
            var subject = CreateSubject();
            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            subject.ConnectionState = new SqLiteConnectionState(connection, transaction);

            Assert.IsNull(subject.Transaction);
        }

        [TestMethod]
        public void Cast_To_IRelationalWorkerNotification_Succeeds()
        {
            var subject = CreateSubject();

            Assert.IsTrue(subject is IRelationalWorkerNotification,
                "SqLiteRelationalWorkerNotification must implement IRelationalWorkerNotification.");
            Assert.IsInstanceOfType<WorkerNotification>(subject);
        }

        [TestMethod]
        public void Plain_WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification()
        {
            var configuration = new TransportConfigurationReceive(
                Substitute.For<IConnectionInformation>(),
                Substitute.For<IQueueDelayFactory>(),
                Substitute.For<IRetryDelayFactory>());
            var plain = new WorkerNotification(
                Substitute.For<IHeaders>(),
                Substitute.For<IQueueCancelWork>(),
                configuration,
                Substitute.For<ILogger>(),
                Substitute.For<IMetrics>(),
                new ActivitySource("test"));

            Assert.IsFalse(plain is IRelationalWorkerNotification,
                "Plain WorkerNotification must NOT implement IRelationalWorkerNotification.");
        }
    }
}
