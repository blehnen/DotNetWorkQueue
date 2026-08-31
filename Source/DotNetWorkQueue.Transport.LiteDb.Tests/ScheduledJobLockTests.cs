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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    /// <summary>
    /// Two properties matter here, and they pull in opposite directions: a scheduled job must
    /// exclude every other scheduled job in the process, and an ordinary send must not be excluded
    /// by anything.
    /// </summary>
    /// <remarks>
    /// Every test starts its worker with a <c>started</c> signal and requires it. Without that, a
    /// worker that never got scheduled looks exactly like a worker blocked on the lock, and the
    /// exclusion test would pass whether or not the lock worked at all.
    /// </remarks>
    [TestClass]
    public class ScheduledJobLockTests
    {
        /// <summary>Generous: these assertions are about blocking, not about speed.</summary>
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        /// <summary>How long to wait before concluding something did *not* happen.</summary>
        private static readonly TimeSpan WaitForNothing = TimeSpan.FromMilliseconds(500);

        [TestMethod]
        [DataRow(null, DisplayName = "no job name")]
        [DataRow("", DisplayName = "empty job name")]
        [DataRow("   ", DisplayName = "blank job name")]
        public void An_Ordinary_Send_Is_Never_Blocked(string jobName)
        {
            //the whole point of the change: an ordinary send used to wait behind every other send
            //in the process, including sends to unrelated queues
            var started = new ManualResetEventSlim(false);
            var acquired = new ManualResetEventSlim(false);

            using (ScheduledJobLock.AcquireIfJob("a job is being queued right now"))
            {
                var worker = Run(jobName, started, acquired);

                Assert.IsTrue(started.Wait(Wait), "the worker never ran");
                Assert.IsTrue(acquired.Wait(Wait), "an ordinary send waited for a scheduled job");
                Assert.IsTrue(worker.Wait(Wait), "the worker never finished");
            }
        }

        [TestMethod]
        public void A_Scheduled_Job_Excludes_Another_Scheduled_Job()
        {
            //the check-then-act this lock exists for: without exclusion both producers can read
            //NotQueued before either commits, and both insert
            var started = new ManualResetEventSlim(false);
            var acquired = new ManualResetEventSlim(false);
            Task worker;

            using (ScheduledJobLock.AcquireIfJob("job-a"))
            {
                worker = Run("job-b", started, acquired);

                Assert.IsTrue(started.Wait(Wait), "the worker never ran");
                Assert.IsFalse(acquired.Wait(WaitForNothing),
                    "two scheduled jobs held the lock at the same time");
            }

            //and it is exclusion rather than deadlock - the worker gets through once released
            Assert.IsTrue(worker.Wait(Wait), "the worker never acquired after the lock was released");
            Assert.IsTrue(acquired.IsSet);
        }

        [TestMethod]
        public void Disposing_A_Scope_Twice_Is_Safe()
        {
            //a scope can be passed around, and a second release would throw
            //SynchronizationLockException if the state lived per copy rather than being shared
            var scope = ScheduledJobLock.AcquireIfJob("job-a");
            var alias = scope;

            scope.Dispose();
            alias.Dispose();

            AssertLockIsFree();
        }

        [TestMethod]
        public void An_Ordinary_Send_Releases_Nothing_And_Throws_Nothing()
        {
            //Dispose has to be safe when no lock was ever taken - Monitor.Exit on an unheld lock
            //throws SynchronizationLockException
            var scope = ScheduledJobLock.AcquireIfJob(null);
            scope.Dispose();
            scope.Dispose();

            AssertLockIsFree();
        }

        [TestMethod]
        public void The_Lock_Is_Released_When_The_Scope_Ends()
        {
            using (ScheduledJobLock.AcquireIfJob("job-a"))
            {
                //held
            }

            AssertLockIsFree();
        }

        /// <summary>Confirms a scheduled job can be queued, which it cannot be if the lock is stuck.</summary>
        private static void AssertLockIsFree()
        {
            var started = new ManualResetEventSlim(false);
            var acquired = new ManualResetEventSlim(false);
            var worker = Run("job-b", started, acquired);

            Assert.IsTrue(started.Wait(Wait), "the worker never ran");
            Assert.IsTrue(acquired.Wait(Wait), "the lock was never released");
            Assert.IsTrue(worker.Wait(Wait), "the worker never finished");
        }

        /// <summary>
        /// Acquires on another thread. Monitor is re-entrant, so a same-thread attempt would
        /// succeed regardless and prove nothing.
        /// </summary>
        private static Task Run(string jobName, ManualResetEventSlim started, ManualResetEventSlim acquired)
            => Task.Run(() =>
            {
                started.Set();
                using (ScheduledJobLock.AcquireIfJob(jobName))
                {
                    acquired.Set();
                }
            });
    }
}
