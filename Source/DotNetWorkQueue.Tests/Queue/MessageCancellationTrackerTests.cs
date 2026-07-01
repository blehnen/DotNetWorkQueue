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
using System.Threading;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class MessageCancellationTrackerTests
    {
        [TestMethod]
        public void Register_Returns_Token()
        {
            var tracker = new MessageCancellationTracker();
            var token = tracker.Register("test-register-1");
            Assert.IsTrue(token.CanBeCanceled);
            tracker.Unregister("test-register-1");
        }

        [TestMethod]
        public void Register_Links_Worker_Tokens()
        {
            var tracker = new MessageCancellationTracker();
            var workerCts = new CancellationTokenSource();
            var token = tracker.Register("test-link-1", workerCts.Token);

            workerCts.Cancel();
            Assert.IsTrue(token.IsCancellationRequested);
            tracker.Unregister("test-link-1");
            workerCts.Dispose();
        }

        [TestMethod]
        public void Cancel_Fires_Token()
        {
            var tracker = new MessageCancellationTracker();
            var token = tracker.Register("test-cancel-1");

            var result = tracker.Cancel("test-cancel-1");
            Assert.IsTrue(result);
            Assert.IsTrue(token.IsCancellationRequested);
            tracker.Unregister("test-cancel-1");
        }

        [TestMethod]
        public void Cancel_Returns_False_For_Unknown_Id()
        {
            var tracker = new MessageCancellationTracker();
            Assert.IsFalse(tracker.Cancel("nonexistent"));
        }

        [TestMethod]
        public void Cancel_Returns_False_When_Already_Canceled()
        {
            var tracker = new MessageCancellationTracker();
            tracker.Register("test-double-cancel-1");

            Assert.IsTrue(tracker.Cancel("test-double-cancel-1"));
            Assert.IsFalse(tracker.Cancel("test-double-cancel-1"));
            tracker.Unregister("test-double-cancel-1");
        }

        [TestMethod]
        public void Unregister_Removes_Token()
        {
            var tracker = new MessageCancellationTracker();
            tracker.Register("test-unregister-1");
            tracker.Unregister("test-unregister-1");

            Assert.IsFalse(tracker.Cancel("test-unregister-1"));
        }

        [TestMethod]
        public void Unregister_Does_Not_Throw_For_Unknown_Id()
        {
            var tracker = new MessageCancellationTracker();
            tracker.Unregister("never-registered");
        }

        [TestMethod]
        public void IsProcessing_Returns_True_When_Registered()
        {
            var tracker = new MessageCancellationTracker();
            tracker.Register("test-processing-1");

            Assert.IsTrue(MessageCancellationTracker.IsProcessing("test-processing-1"));
            tracker.Unregister("test-processing-1");
            Assert.IsFalse(MessageCancellationTracker.IsProcessing("test-processing-1"));
        }
    }
}
