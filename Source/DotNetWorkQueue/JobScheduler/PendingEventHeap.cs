//Copyright(c) 2015 Bret Copeland<bret@atlantisflight.org>
//
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of
//this software and associated documentation files (the "Software"), to deal in 
//the Software without restriction, including without limitation the rights to
//use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
// 
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
//FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
//IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System;
using DotNetWorkQueue.Exceptions;

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
