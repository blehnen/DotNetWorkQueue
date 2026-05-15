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
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    /// <summary>
    /// Phase 5 negative-path coverage: the LiteDb transport is non-relational and MUST NOT
    /// accidentally implement the outbox interface <see cref="IRelationalProducerQueue{T}"/>
    /// shipped in Phase 2 and consumed by Phases 3 (SqlServer) and 4 (PostgreSQL).
    /// </summary>
    [TestClass]
    public class LiteDbProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void LiteDb_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. LiteDb resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "LiteDb transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. LiteDb is a non-relational transport with no " +
                "external-transaction outbox surface.");

            // Decision 2: reflection-based assembly assertion. Scan the LiteDb transport
            // assembly (anchored on LiteDbMessageQueueInit — RESEARCH §1) for ANY type
            // implementing the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(LiteDbMessageQueueInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"LiteDb transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");
        }
    }
}
