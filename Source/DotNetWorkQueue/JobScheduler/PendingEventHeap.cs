// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.JobScheduler
{
    internal class PendingEventHeap
    {
        private PendingEvent[] _events = new PendingEvent[16];

        public int Count { get; private set; }

        public void Push(PendingEvent ev)
        {
            Guard.NotNull(() => ev, ev);
            var ei = Add(ev);
            while (ei > 0)
            {
                var pi = ParentIndex(ei);
                if (ev.IsEarlierThan(_events[pi]))
                {
                    Swap(ei, pi);
                    ei = pi;
                }
                else
                {
                    break;
                }
            }
        }

        public PendingEvent Pop()
        {
            if (Count == 0)
                throw new JobSchedulerException("Pop called on empty heap.");

            var ret = _events[0];
            var end = ClearEndElement();

            if (Count <= 0) return ret;

            _events[0] = end;
            var ei = 0;

            while (true)
            {
                var lci = LeftChildIndex(ei);
                var rci = RightChildIndex(ei);

                if (lci < Count && _events[lci].IsEarlierThan(end))
                {
                    // we know the left child is earlier than the parent, but we have to check if the right child is actually the correct parent
                    if (rci < Count && _events[rci].IsEarlierThan(_events[lci]))
                    {
                        // right child is earlier than left child, so it's the correct parent
                        Swap(ei, rci);
                        ei = rci;
                    }
                    else
                    {
                        // left is the correct parent
                        Swap(ei, lci);
                        ei = lci;
                    }
                }
                else if (rci < Count && _events[rci].IsEarlierThan(end))
                {
                    // only the right child is earlier than the parent, so we know that's the one to swap
                    Swap(ei, rci);
                    ei = rci;
                }
                else
                {
                    break;
                }
            }

            return ret;
        }

        public PendingEvent Peek()
        {
            return Count > 0 ? _events[0] : null;
        }

        private static int ParentIndex(int index)
        {
            return (index - 1) / 2;
        }

        private static int LeftChildIndex(int index)
        {
            return 2 * (index + 1);
        }

        private static int RightChildIndex(int index)
        {
            return 2 * (index + 1) - 1;
        }

        private void Swap(int a, int b)
        {
            var temp = _events[a];
            _events[a] = _events[b];
            _events[b] = temp;
        }

        // returns the index of the added item
        private int Add(PendingEvent ev)
        {
            // check if we need to resize
            if (_events.Length == Count)
            {
                var bigger = new PendingEvent[_events.Length * 2];
                Array.Copy(_events, bigger, Count);
                _events = bigger;
            }

            _events[Count] = ev;
            return Count++; // postfix is intentional
        }

        private PendingEvent ClearEndElement()
        {
            Count--;
            var ev = _events[Count];
            _events[Count] = null; // so the GC can release it
            return ev;
        }
    }
}
