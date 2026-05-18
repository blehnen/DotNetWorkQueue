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
using System.Data.Common;
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    /// <summary>
    /// Contract + behavior tests for <see cref="SqlServerRelationalWorkerNotification"/>.
    /// Pairs with <see cref="SqlServerRelationalWorkerNotificationRegistrationTests"/> for the
    /// option-driven SimpleInjector smoke tests.
    /// </summary>
    [TestClass]
    public class SqlServerRelationalWorkerNotificationTests
    {
        private static SqlServerRelationalWorkerNotification CreateSubject(
            out IHeaders headers,
            out IQueueCancelWork cancelWork,
            out TransportConfigurationReceive configuration,
            out ILogger log,
            out IMetrics metrics,
            out ActivitySource tracer)
        {
            headers = Substitute.For<IHeaders>();
            cancelWork = Substitute.For<IQueueCancelWork>();
            configuration = new TransportConfigurationReceive(
                Substitute.For<IConnectionInformation>(),
                Substitute.For<IQueueDelayFactory>(),
                Substitute.For<IRetryDelayFactory>())
            { MessageRollbackSupported = true };
            log = Substitute.For<ILogger>();
            metrics = Substitute.For<IMetrics>();
            tracer = new ActivitySource("test");
            return new SqlServerRelationalWorkerNotification(headers, cancelWork, configuration, log, metrics, tracer);
        }

        [TestMethod]
        public void Constructor_Passes_Args_To_Base()
        {
            var subject = CreateSubject(out var headers, out var cancelWork, out _, out var log, out var metrics, out var tracer);

            Assert.AreSame(headers, subject.HeaderNames);
            Assert.AreSame(cancelWork, subject.WorkerStopping);
            Assert.AreSame(log, subject.Log);
            Assert.AreSame(metrics, subject.Metrics);
            Assert.AreSame(tracer, subject.Tracer);
            Assert.IsTrue(subject.TransportSupportsRollback);
        }

        [TestMethod]
        public void Transaction_Returns_Null_When_ConnectionHolder_Not_Set()
        {
            var subject = CreateSubject(out _, out _, out _, out _, out _, out _);

            Assert.IsNull(subject.Transaction);
        }

        [TestMethod]
        public void Transaction_Returns_Null_When_ConnectionHolder_Transaction_Is_Null()
        {
            var subject = CreateSubject(out _, out _, out _, out _, out _, out _);
            var holder = Substitute.For<IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>>();
            holder.Transaction.Returns((SqlTransaction)null);
            subject.ConnectionHolder = holder;

            Assert.IsNull(subject.Transaction);
        }

        [TestMethod]
        public void ConnectionHolder_PropertySet_Does_Not_Throw()
        {
            // SqlTransaction is sealed → NSubstitute cannot proxy it, so the non-null-return path of
            // Transaction => ConnectionHolder?.Transaction cannot be unit-tested at this level. The
            // delegation contract ("getter forwards ConnectionHolder.Transaction with no transformation")
            // is proven by Transaction_Returns_Null_When_ConnectionHolder_Transaction_Is_Null. This
            // test only confirms that assigning a non-null IConnectionHolder does not throw and the
            // property round-trips. Full non-null-return coverage lands in Phase 7 SqlServer
            // integration tests where a real SqlTransaction is available.
            var subject = CreateSubject(out _, out _, out _, out _, out _, out _);
            var holder = Substitute.For<IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>>();

            subject.ConnectionHolder = holder;

            Assert.IsNotNull(subject.ConnectionHolder);
            Assert.AreSame(holder, subject.ConnectionHolder);
        }

        [TestMethod]
        public void Cast_To_IRelationalWorkerNotification_Succeeds()
        {
            var subject = CreateSubject(out _, out _, out _, out _, out _, out _);

            Assert.IsTrue(subject is IRelationalWorkerNotification, "SqlServerRelationalWorkerNotification must implement IRelationalWorkerNotification.");
            Assert.IsInstanceOfType<WorkerNotification>(subject);
        }

        [TestMethod]
        public void Plain_WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification()
        {
            var headers = Substitute.For<IHeaders>();
            var cancelWork = Substitute.For<IQueueCancelWork>();
            var configuration = new TransportConfigurationReceive(
                Substitute.For<IConnectionInformation>(),
                Substitute.For<IQueueDelayFactory>(),
                Substitute.For<IRetryDelayFactory>());
            var log = Substitute.For<ILogger>();
            var metrics = Substitute.For<IMetrics>();
            var tracer = new ActivitySource("test");
            var plain = new WorkerNotification(headers, cancelWork, configuration, log, metrics, tracer);

            Assert.IsFalse(plain is IRelationalWorkerNotification, "Plain WorkerNotification must NOT implement IRelationalWorkerNotification (capability-cast sanity).");
        }
    }
}
