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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.JobScheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.JobScheduler
{
    /// <summary>
    /// Tests for the job scheduler's transport-queue cache.
    /// </summary>
    /// <remarks>
    /// The queue-creation success paths call into a real transport container and cannot be
    /// reached without a transport project reference, so these tests target the guard,
    /// dispose, and creation-failure branches.
    /// </remarks>
    [TestClass]
    public class JobQueueTests
    {
        private static JobQueueContainerRegistrations EmptyRegistrations()
        {
            // every argument is null-tolerant and defaults to a no-op lambda
            return new JobQueueContainerRegistrations(null, null, null, null);
        }

        private static JobQueue Create()
        {
            return new JobQueue(EmptyRegistrations());
        }

        // --- construction ---

        [TestMethod]
        public void Constructor_NullRegistrations_Throws()
        {
            Action act = () => new JobQueue(null);
            Assert.Throws<ArgumentNullException>(act);
        }

        [TestMethod]
        public void Constructor_ValidRegistrations_IsNotDisposed()
        {
            using var queue = Create();
            Assert.IsFalse(queue.IsDisposed);
        }

        // --- dispose ---

        [TestMethod]
        public void Dispose_MarksInstanceDisposed()
        {
            var queue = Create();
            queue.Dispose();

            Assert.IsTrue(queue.IsDisposed);
        }

        [TestMethod]
        public void Dispose_CalledTwice_IsIdempotent()
        {
            var queue = Create();
            queue.Dispose();
            queue.Dispose();

            Assert.IsTrue(queue.IsDisposed);
        }

        [TestMethod]
        public void Dispose_WithNothingCached_DoesNotThrow()
        {
            // exercises the dispose loops against empty queue/scope/container collections
            var queue = Create();
            queue.Dispose();

            Assert.IsTrue(queue.IsDisposed);
        }

        // --- creation failure ---

        [TestMethod]
        public void Get_CreationReportsFailure_ThrowsWithErrorMessage()
        {
            using var queue = Create();
            var creation = Substitute.For<IJobQueueCreation>();
            creation.CreateJobSchedulerQueue(Arg.Any<Action<IContainer>>(), Arg.Any<QueueConnection>(),
                    Arg.Any<Action<IContainer>>(), Arg.Any<bool>())
                .Returns(new QueueCreationResult(QueueCreationStatus.ConfigurationError, "the queue blew up"));

            Action act = () => queue.Get<NoOpTransport>(creation, new QueueConnection("aQueue", "aConnection"));

            var exception = Assert.Throws<DotNetWorkQueueException>(act);
            StringAssert.Contains(exception.Message, "the queue blew up");
        }
    }
}
