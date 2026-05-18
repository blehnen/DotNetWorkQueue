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
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic
{
    /// <summary>
    /// Phase 5 negative-path coverage: the Memory transport is non-relational and MUST NOT
    /// accidentally implement the outbox interface <see cref="IRelationalProducerQueue{T}"/>
    /// shipped in Phase 2 and consumed by Phases 3 (SqlServer) and 4 (PostgreSQL).
    /// </summary>
    [TestClass]
    public class MemoryProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void Memory_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. Memory resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "Memory transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. Memory is a non-relational transport with no " +
                "external-transaction outbox surface.");

            // Decision 2: reflection-based assembly assertion. Scan the Memory-specific NuGet
            // assembly (anchored on MemoryDashboardInit — RESEARCH §1) for ANY type implementing
            // the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(MemoryDashboardInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"Memory transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");
        }

        [TestMethod]
        public void Memory_WorkerNotification_DoesNotImplement_IRelationalWorkerNotification()
        {
            // Phase 6 negative-path coverage for the inbox capability-cast pattern: the Memory
            // transport's IWorkerNotification resolves to the core WorkerNotification, which
            // must NOT implement IRelationalWorkerNotification (PROJECT.md §Success Criteria #3).
            Assert.IsFalse(
                typeof(IRelationalWorkerNotification).IsAssignableFrom(typeof(WorkerNotification)),
                "Memory transport invariant violated: core WorkerNotification must NOT implement " +
                "IRelationalWorkerNotification. The inbox capability cast is intended to cleanly " +
                "fail on non-relational transports.");

            // Assembly scan: no type in the Memory transport assembly should implement
            // IRelationalWorkerNotification.
            var transportAssembly = typeof(MemoryDashboardInit).Assembly;
            var anyImplementsRelational = transportAssembly.GetTypes()
                .Any(t => typeof(IRelationalWorkerNotification).IsAssignableFrom(t));
            Assert.IsFalse(anyImplementsRelational,
                $"Memory transport assembly '{transportAssembly.GetName().Name}' must NOT " +
                "contain any type implementing IRelationalWorkerNotification.");
        }
    }
}
