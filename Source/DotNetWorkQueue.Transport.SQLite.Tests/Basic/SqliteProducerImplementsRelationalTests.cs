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
using System.Linq;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Positive-path coverage introduced by issue #149 (SQLite outbox support).
    /// Phase 2 of the SQLite outbox milestone replaced the prior negative-guard file
    /// (SqliteProducerDoesNotImplementRelationalTests) with these assertions that confirm
    /// the SQLite transport NOW exposes the relational outbox capability via
    /// <see cref="SqliteRelationalProducerQueue{T}"/>.
    /// </summary>
    [TestClass]
    public class SqliteProducerImplementsRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void Assembly_ContainsAtLeastOneIRelationalProducerQueueImplementor()
        {
            // Issue #149 added SqliteRelationalProducerQueue<T> to the SQLite transport
            // assembly. This assembly scan is the direct inverse of the deleted negative-guard
            // assertion that previously required zero implementors. Anchored on
            // SqLiteMessageQueueInit per existing SQLite test convention.
            var transportAssembly = typeof(SqLiteMessageQueueInit).Assembly;
            var anyImplementsRelational = transportAssembly.GetTypes().Any(t =>
                !t.IsAbstract &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));

            Assert.IsTrue(anyImplementsRelational,
                $"SQLite transport assembly '{transportAssembly.GetName().Name}' must contain " +
                "at least one non-abstract type implementing IRelationalProducerQueue<T> " +
                "(issue #149 enabled outbox support).");
        }

        [TestMethod]
        public void SqliteRelationalProducerQueue_IsAssignableTo_IRelationalProducerQueue()
        {
            // Issue #149 introduced SqliteRelationalProducerQueue<T> which implements
            // IRelationalProducerQueue<T> so callers can obtain an IRelationalProducerQueue<T>
            // from the SQLite DI container and enqueue with outbox semantics.
            Assert.IsTrue(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqliteRelationalProducerQueue<TestMessage>)),
                "SqliteRelationalProducerQueue<T> must implement IRelationalProducerQueue<T>.");
        }

        [TestMethod]
        public void SqliteRelationalProducerQueue_DerivesFrom_RelationalProducerQueueBase()
        {
            // SqliteRelationalProducerQueue<T> inherits from RelationalProducerQueue<T>,
            // the shared base that carries the outbox transaction logic across all relational
            // transports. This guards the inheritance hierarchy established in issue #149.
            Assert.IsTrue(
                typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqliteRelationalProducerQueue<TestMessage>)),
                "SqliteRelationalProducerQueue<T> must derive from RelationalProducerQueue<T>.");
        }

        [TestMethod]
        public void SqliteRelationalProducerQueue_IsAssignableTo_IProducerQueue()
        {
            // SqliteRelationalProducerQueue<T> must remain a valid IProducerQueue<T> so
            // existing producer consumers that only know IProducerQueue<T> continue to work
            // after the issue #149 outbox promotion.
            Assert.IsTrue(
                typeof(IProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqliteRelationalProducerQueue<TestMessage>)),
                "SqliteRelationalProducerQueue<T> must implement IProducerQueue<T>.");
        }
    }
}
