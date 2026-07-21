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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.JobScheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.JobScheduler
{
    /// <summary>
    /// Tests for the binary min-heap backing the job scheduler's pending-event queue.
    /// </summary>
    /// <remarks>
    /// The heap only ever reads <see cref="PendingEvent.ScheduledTime"/>, so events are
    /// built with a null job — constructing a real scheduler would add nothing. All times
    /// are fixed offsets from a constant base; nothing here sleeps or reads the clock.
    /// </remarks>
    [TestClass]
    public class PendingEventHeapTests
    {
        private static readonly DateTimeOffset Base =
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>Builds an event scheduled <paramref name="offsetSeconds"/> after the base time.</summary>
        private static PendingEvent Ev(int offsetSeconds)
        {
            return new PendingEvent(Base.AddSeconds(offsetSeconds), null, offsetSeconds);
        }

        /// <summary>Pushes the given offsets, then pops everything and asserts ascending order.</summary>
        private static void AssertPopsAscending(params int[] offsets)
        {
            var heap = new PendingEventHeap();
            foreach (var offset in offsets)
            {
                heap.Push(Ev(offset));
            }

            Assert.AreEqual(offsets.Length, heap.Count);

            var popped = new List<int>();
            while (heap.Count > 0)
            {
                popped.Add((int)(heap.Pop().ScheduledTime - Base).TotalSeconds);
            }

            CollectionAssert.AreEqual(offsets.OrderBy(o => o).ToList(), popped,
                "heap must yield events in ascending scheduled-time order");
            Assert.AreEqual(0, heap.Count);
        }

        // --- empty-heap behavior ---

        [TestMethod]
        public void Count_NewHeap_IsZero()
        {
            Assert.AreEqual(0, new PendingEventHeap().Count);
        }

        [TestMethod]
        public void Peek_EmptyHeap_ReturnsNull()
        {
            Assert.IsNull(new PendingEventHeap().Peek());
        }

        [TestMethod]
        public void Pop_EmptyHeap_Throws()
        {
            var heap = new PendingEventHeap();
            Action act = () => heap.Pop();
            Assert.Throws<JobSchedulerException>(act);
        }

        [TestMethod]
        public void Pop_AfterDrainingHeap_Throws()
        {
            var heap = new PendingEventHeap();
            heap.Push(Ev(1));
            heap.Pop();

            Action act = () => heap.Pop();
            Assert.Throws<JobSchedulerException>(act);
        }

        [TestMethod]
        public void Push_Null_Throws()
        {
            var heap = new PendingEventHeap();
            Action act = () => heap.Push(null);
            Assert.Throws<ArgumentNullException>(act);
        }

        // --- push / sift-up ---

        [TestMethod]
        public void Push_SingleEvent_IsPeekedAndCounted()
        {
            var heap = new PendingEventHeap();
            var ev = Ev(5);
            heap.Push(ev);

            Assert.AreEqual(1, heap.Count);
            Assert.AreSame(ev, heap.Peek());
        }

        [TestMethod]
        public void Push_LaterEventAfterEarlier_DoesNotDisplaceRoot()
        {
            var heap = new PendingEventHeap();
            var first = Ev(1);
            heap.Push(first);
            heap.Push(Ev(9));

            Assert.AreEqual(2, heap.Count);
            Assert.AreSame(first, heap.Peek(), "the earlier event stays at the root");
        }

        [TestMethod]
        public void Push_EarlierEvent_SiftsToRoot()
        {
            var heap = new PendingEventHeap();
            heap.Push(Ev(9));
            var earliest = Ev(1);
            heap.Push(earliest);

            Assert.AreSame(earliest, heap.Peek());
        }

        [TestMethod]
        public void Push_DescendingSequence_SiftsThroughMultipleLevels()
        {
            // each push is earlier than everything already present, so every insert
            // bubbles all the way from a leaf to the root (multi-iteration sift-up)
            var heap = new PendingEventHeap();
            for (var offset = 20; offset >= 1; offset--)
            {
                heap.Push(Ev(offset));
                Assert.AreEqual(offset, (int)(heap.Peek().ScheduledTime - Base).TotalSeconds);
            }

            Assert.AreEqual(20, heap.Count);
        }

        [TestMethod]
        public void Push_BeyondInitialCapacity_ResizesAndPreservesOrder()
        {
            // the backing array starts at 16 — pushing more forces the grow-and-copy path
            AssertPopsAscending(Enumerable.Range(1, 40).Reverse().ToArray());
        }

        // --- pop / sift-down ---

        [TestMethod]
        public void Pop_SingleEvent_ReturnsItAndEmptiesHeap()
        {
            var heap = new PendingEventHeap();
            var ev = Ev(3);
            heap.Push(ev);

            Assert.AreSame(ev, heap.Pop());
            Assert.AreEqual(0, heap.Count);
            Assert.IsNull(heap.Peek());
        }

        [TestMethod]
        public void Pop_ReturnsEarliestEventInstance()
        {
            var heap = new PendingEventHeap();
            var earliest = Ev(2);
            heap.Push(Ev(7));
            heap.Push(earliest);
            heap.Push(Ev(5));

            Assert.AreSame(earliest, heap.Pop());
            Assert.AreEqual(2, heap.Count);
        }

        [TestMethod]
        public void PopAll_AscendingInsertOrder_ReturnsAscending()
        {
            AssertPopsAscending(1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        [TestMethod]
        public void PopAll_DescendingInsertOrder_ReturnsAscending()
        {
            AssertPopsAscending(9, 8, 7, 6, 5, 4, 3, 2, 1);
        }

        [TestMethod]
        public void PopAll_MixedInsertOrder_ReturnsAscending()
        {
            AssertPopsAscending(5, 12, 1, 9, 3, 15, 7, 2, 11, 4, 8, 6, 14, 10, 13);
        }

        [TestMethod]
        public void PopAll_DuplicateScheduledTimes_ReturnsAllInAscendingOrder()
        {
            AssertPopsAscending(4, 1, 4, 2, 1, 3, 2, 4);
        }

        [TestMethod]
        public void PopAll_TwoEvents_ReturnsAscending()
        {
            AssertPopsAscending(2, 1);
        }

        [TestMethod]
        public void PopAll_ThreeEvents_ReturnsAscending()
        {
            AssertPopsAscending(3, 1, 2);
        }

        // --- peek ---

        [TestMethod]
        public void Peek_CalledRepeatedly_DoesNotMutateHeap()
        {
            var heap = new PendingEventHeap();
            heap.Push(Ev(6));
            heap.Push(Ev(2));
            heap.Push(Ev(4));

            var first = heap.Peek();
            var second = heap.Peek();

            Assert.AreSame(first, second);
            Assert.AreEqual(3, heap.Count, "peek must not remove anything");
            Assert.AreEqual(2, (int)(first.ScheduledTime - Base).TotalSeconds);
        }

        // --- interleaved use ---

        [TestMethod]
        public void PushAndPop_Interleaved_AlwaysYieldsCurrentEarliest()
        {
            var heap = new PendingEventHeap();
            heap.Push(Ev(10));
            heap.Push(Ev(20));

            Assert.AreEqual(10, (int)(heap.Pop().ScheduledTime - Base).TotalSeconds);

            heap.Push(Ev(5));
            heap.Push(Ev(30));

            Assert.AreEqual(5, (int)(heap.Pop().ScheduledTime - Base).TotalSeconds);
            Assert.AreEqual(20, (int)(heap.Pop().ScheduledTime - Base).TotalSeconds);
            Assert.AreEqual(30, (int)(heap.Pop().ScheduledTime - Base).TotalSeconds);
            Assert.AreEqual(0, heap.Count);
        }
    }
}
