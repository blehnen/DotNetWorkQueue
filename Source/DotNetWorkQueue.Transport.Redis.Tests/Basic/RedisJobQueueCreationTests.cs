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
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisJobQueueCreationTests
    {
        [TestMethod]
        public void Constructor_NullCreation_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new RedisJobQueueCreation(null));
        }

        [TestMethod]
        public void IsDisposed_Delegates_ToInnerCreation()
        {
            var inner = Substitute.For<IQueueCreation>();
            inner.IsDisposed.Returns(true);
            var sut = new RedisJobQueueCreation(inner);
            Assert.IsTrue(sut.IsDisposed);
        }

        [TestMethod]
        public void Scope_Delegates_ToInnerCreation()
        {
            var inner = Substitute.For<IQueueCreation>();
            var scope = Substitute.For<ICreationScope>();
            inner.Scope.Returns(scope);
            var sut = new RedisJobQueueCreation(inner);
            Assert.AreSame(scope, sut.Scope);
        }

        [TestMethod]
        public void CreateJobSchedulerQueue_Delegates_ToInnerCreateQueue()
        {
            var inner = Substitute.For<IQueueCreation>();
            var expected = new QueueCreationResult(QueueCreationStatus.Success);
            inner.CreateQueue().Returns(expected);
            var sut = new RedisJobQueueCreation(inner);
            var result = sut.CreateJobSchedulerQueue(_ => { }, new QueueConnection("queue", "connection"));
            Assert.AreSame(expected, result);
            inner.Received(1).CreateQueue();
        }

        [TestMethod]
        public void RemoveQueue_Delegates_ToInnerRemoveQueue()
        {
            var inner = Substitute.For<IQueueCreation>();
            var expected = new QueueRemoveResult(QueueRemoveStatus.Success);
            inner.RemoveQueue().Returns(expected);
            var sut = new RedisJobQueueCreation(inner);
            var result = sut.RemoveQueue();
            Assert.AreSame(expected, result);
            inner.Received(1).RemoveQueue();
        }
    }
}
