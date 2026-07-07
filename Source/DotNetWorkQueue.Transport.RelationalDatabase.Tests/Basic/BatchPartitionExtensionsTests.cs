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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class BatchPartitionExtensionsTests
    {
        [TestMethod]
        public void SplitsAtBoundary_WithRemainder()
        {
            var source = Enumerable.Range(1, 2500).ToList();

            var chunks = source.Partition(1000).ToList();

            Assert.HasCount(3, chunks);
            Assert.HasCount(1000, chunks[0]);
            Assert.HasCount(1000, chunks[1]);
            Assert.HasCount(500, chunks[2]);
        }

        [TestMethod]
        public void ExactMultiple_ProducesEvenChunks()
        {
            var source = Enumerable.Range(1, 2000).ToList();

            var chunks = source.Partition(1000).ToList();

            Assert.HasCount(2, chunks);
            Assert.HasCount(1000, chunks[0]);
            Assert.HasCount(1000, chunks[1]);
        }

        [TestMethod]
        public void ListSmallerThanSize_YieldsSingleChunk()
        {
            var source = Enumerable.Range(1, 10).ToList();

            var chunks = source.Partition(1000).ToList();

            Assert.HasCount(1, chunks);
            Assert.HasCount(10, chunks[0]);
        }

        [TestMethod]
        public void EmptySource_YieldsNoChunks()
        {
            var source = new List<int>();

            var chunks = source.Partition(1000).ToList();

            Assert.IsEmpty(chunks);
        }

        [TestMethod]
        public void PreservesOrderAcrossChunks()
        {
            var source = Enumerable.Range(1, 2500).ToList();

            var flattened = source.Partition(1000).SelectMany(c => c).ToList();

            CollectionAssert.AreEqual(source, flattened);
        }

        [TestMethod]
        public void SizeBelowOne_Throws()
        {
            var source = Enumerable.Range(1, 10).ToList();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.Partition(0).ToList());
        }

        [TestMethod]
        public void NullSource_Throws()
        {
            IReadOnlyList<int> source = null;
            Assert.ThrowsExactly<ArgumentNullException>(() => source.Partition(10).ToList());
        }
    }
}
