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
using System.Reflection;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class LiteDbSendJobToQueueTests
    {
        [TestMethod]
        public void Constructor_AssignsDependenciesWithoutThrowing()
        {
            // Arrange / Act
            var sut = CreateSut();

            // Assert
            Assert.IsNotNull(sut);
            Assert.IsInstanceOfType(sut, typeof(ASendJobToQueue));
        }

        [TestMethod]
        public void DeleteJob_RetrievesJobIdAndRemovesMessageWithErrorReason()
        {
            // Arrange
            const int expectedId = 4242;
            var getJobId = Substitute.For<IQueryHandler<GetJobIdQuery<int>, int>>();
            getJobId.Handle(Arg.Any<GetJobIdQuery<int>>()).Returns(expectedId);
            var removeMessage = Substitute.For<IRemoveMessage>();

            var sut = CreateSut(getJobId: getJobId, removeMessage: removeMessage);

            // Act
            InvokeDeleteJob(sut, "jobToDelete");

            // Assert
            getJobId.Received(1).Handle(Arg.Is<GetJobIdQuery<int>>(q => q.JobName == "jobToDelete"));
            removeMessage.Received(1).Remove(
                Arg.Is<IMessageId>(id => id is MessageQueueId<int> && (int)((MessageQueueId<int>)id).Id.Value == expectedId),
                RemoveMessageReason.Error);
        }

        [TestMethod]
        public void JobAlreadyExistsError_NullError_ReturnsFalse()
        {
            // Arrange
            var sut = CreateSut();

            // Act
            var result = InvokeJobAlreadyExistsError(sut, null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void JobAlreadyExistsError_MatchingMessage_ReturnsTrue()
        {
            // Arrange
            var sut = CreateSut();
            var error = new InvalidOperationException(
                "Failed to insert record - the job has already been queued or processed");

            // Act
            var result = InvokeJobAlreadyExistsError(sut, error);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void JobAlreadyExistsError_UnrelatedMessage_ReturnsFalse()
        {
            // Arrange
            var sut = CreateSut();
            var error = new InvalidOperationException("some other database error");

            // Act
            var result = InvokeJobAlreadyExistsError(sut, error);

            // Assert
            Assert.IsFalse(result);
        }

        #region Helpers

        // NOTE: We deliberately do not exercise DoesJobExist here because it
        // calls _connectionInformation.GetDatabase() on a concrete
        // LiteDbConnectionManager which opens a real LiteDatabase. That would
        // make this a file/integration test. A null connection manager is
        // sufficient for the constructor, DeleteJob, and JobAlreadyExistsError
        // paths which never touch it.
        private static LiteDbSendJobToQueue CreateSut(
            LiteDbConnectionManager connectionInformation = null,
            IProducerMethodQueue queue = null,
            IQueryHandler<DoesJobExistQuery, QueueStatuses> doesJobExist = null,
            IRemoveMessage removeMessage = null,
            IQueryHandler<GetJobIdQuery<int>, int> getJobId = null,
            CreateJobMetaData createJobMetaData = null,
            IGetTimeFactory getTimeFactory = null)
        {
            return new LiteDbSendJobToQueue(
                connectionInformation,
                queue ?? Substitute.For<IProducerMethodQueue>(),
                doesJobExist ?? Substitute.For<IQueryHandler<DoesJobExistQuery, QueueStatuses>>(),
                removeMessage ?? Substitute.For<IRemoveMessage>(),
                getJobId ?? Substitute.For<IQueryHandler<GetJobIdQuery<int>, int>>(),
                createJobMetaData ?? new CreateJobMetaData(Substitute.For<IJobSchedulerMetaData>()),
                getTimeFactory ?? Substitute.For<IGetTimeFactory>());
        }

        private static void InvokeDeleteJob(LiteDbSendJobToQueue sut, string name)
        {
            var method = typeof(LiteDbSendJobToQueue).GetMethod("DeleteJob",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(sut, new object[] { name });
        }

        private static bool InvokeJobAlreadyExistsError(LiteDbSendJobToQueue sut, Exception error)
        {
            var method = typeof(LiteDbSendJobToQueue).GetMethod("JobAlreadyExistsError",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)method.Invoke(sut, new object[] { error });
        }

        #endregion
    }
}
