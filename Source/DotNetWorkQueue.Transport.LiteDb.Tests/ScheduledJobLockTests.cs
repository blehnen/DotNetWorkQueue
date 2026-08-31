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
    [TestClass]
    public class ScheduledJobLockTests
    {
        //generous, because the assertions are about blocking rather than about speed
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(2);

        [TestMethod]
        [DataRow(null, DisplayName = "no job name")]
        [DataRow("", DisplayName = "empty job name")]
        [DataRow("   ", DisplayName = "blank job name")]
        public void An_Ordinary_Send_Is_Never_Blocked(string jobName)
        {
            //this is the whole point of the change - an ordinary send used to wait behind every
            //other send in the process, including sends to unrelated queues
            using var held = ScheduledJobLock.AcquireIfJob("a job is being queued right now");

            Assert.IsTrue(TryAcquireOnAnotherThread(jobName),
                "an ordinary send waited for a scheduled job to finish");
        }

        [TestMethod]
        public void A_Scheduled_Job_Excludes_Another_Scheduled_Job()
        {
            //the check-then-act this lock exists for: without exclusion both producers can read
            //NotQueued before either commits, and both insert
            using var held = ScheduledJobLock.AcquireIfJob("job-a");

            Assert.IsFalse(TryAcquireOnAnotherThread("job-b"),
                "two scheduled jobs were queued concurrently");
        }

        [TestMethod]
        public void The_Lock_Is_Released_When_The_Scope_Ends()
        {
            using (ScheduledJobLock.AcquireIfJob("job-a"))
            {
                //held
            }

            Assert.IsTrue(TryAcquireOnAnotherThread("job-b"), "the lock outlived its scope");
        }

        [TestMethod]
        public void An_Ordinary_Send_Releases_Nothing_And_Throws_Nothing()
        {
            //Dispose has to be safe when no lock was ever taken - Monitor.Exit on an unheld lock
            //would throw SynchronizationLockException
            using (ScheduledJobLock.AcquireIfJob(null))
            {
                //nothing taken
            }
        }

        /// <summary>
        /// Tries to acquire on a thread other than the caller's, and reports whether it got through
        /// rather than blocking. Monitor is re-entrant, so this cannot be done on the same thread.
        /// </summary>
        private static bool TryAcquireOnAnotherThread(string jobName)
        {
            var acquired = new ManualResetEventSlim(false);
            var release = new ManualResetEventSlim(false);

            var worker = Task.Run(() =>
            {
                using (ScheduledJobLock.AcquireIfJob(jobName))
                {
                    acquired.Set();
                    release.Wait(Wait);
                }
            });

            var gotThrough = acquired.Wait(Wait);
            release.Set();
            worker.Wait(Wait);
            return gotThrough;
        }
    }
}
