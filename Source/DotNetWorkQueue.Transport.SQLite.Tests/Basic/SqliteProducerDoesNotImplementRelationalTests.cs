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
    /// Phase 5 negative-path coverage: the SQLite transport is the closest non-relational
    /// transport in shape to the SqlServer/PostgreSQL relational transports (ROADMAP §Phase 5
    /// flags SQLite as the explicitly-deferred relational case). The Decision-4 extra
    /// assertion below specifically guards against an accidental inheritance from
    /// <see cref="RelationalProducerQueue{T}"/>.
    /// </summary>
    [TestClass]
    public class SqliteProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void Sqlite_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. SQLite resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "SQLite transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. SQLite's outbox surface is explicitly deferred.");

            // Decision 2: reflection-based assembly assertion. Scan the SQLite transport
            // assembly (anchored on SqLiteMessageQueueInit — RESEARCH §1) for ANY type
            // implementing the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(SqLiteMessageQueueInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"SQLite transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");

            // Decision 4 (SQLite-only extra): SQLite is the explicitly-deferred relational
            // case (ROADMAP §Phase 5). Defend against the "accidentally inherits from
            // RelationalProducerQueue<T>" misconfiguration. By construction this asserts that
            // ProducerQueue<T> is NOT assignable to RelationalProducerQueue<T> — the
            // inheritance goes the other way (RelationalProducerQueue<T> : ProducerQueue<T>),
            // so this assertion is true by the current type hierarchy. The regression value
            // is future-proofing: if a SQLite-specific subclass is ever introduced that
            // derives from RelationalProducerQueue<T>, the assembly assertion above would
            // also catch it — this extra check makes the SQLite-specific intent explicit.
            Assert.IsFalse(
                typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "SQLite transport invariant violated: ProducerQueue<T> must NOT derive from " +
                "RelationalProducerQueue<T>. SQLite's outbox surface is explicitly deferred " +
                "and must not accidentally pick up the relational base.");
        }
    }
}
