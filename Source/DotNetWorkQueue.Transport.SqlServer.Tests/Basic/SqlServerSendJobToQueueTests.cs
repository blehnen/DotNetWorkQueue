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
using Microsoft.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlServerSendJobToQueueTests
    {
        [TestMethod]
        public void DoesJobExist_DelegatesToQueryHandler_ReturnsResult()
        {
            // Arrange
            var doesJobExist = Substitute.For<IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses>>();
            doesJobExist.Handle(Arg.Any<DoesJobExistQuery<SqlConnection, SqlTransaction>>())
                .Returns(QueueStatuses.Processing);

            var sut = CreateSut(doesJobExist: doesJobExist);

            // Act
            var result = InvokeDoesJobExist(sut, "myJob", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

            // Assert
            Assert.AreEqual(QueueStatuses.Processing, result);
            doesJobExist.Received(1).Handle(Arg.Any<DoesJobExistQuery<SqlConnection, SqlTransaction>>());
        }

        [TestMethod]
        public void DoesJobExist_PassesCorrectQueryArguments()
        {
            // Arrange
            var doesJobExist = Substitute.For<IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses>>();
            doesJobExist.Handle(Arg.Any<DoesJobExistQuery<SqlConnection, SqlTransaction>>())
                .Returns(QueueStatuses.Waiting);

            var sut = CreateSut(doesJobExist: doesJobExist);
            var name = "myJobName";
            var scheduled = new DateTimeOffset(2026, 4, 12, 10, 30, 0, TimeSpan.Zero);

            // Act
            InvokeDoesJobExist(sut, name, scheduled);

            // Assert
            doesJobExist.Received(1).Handle(Arg.Is<DoesJobExistQuery<SqlConnection, SqlTransaction>>(q =>
                q.JobName == name && q.ScheduledTime == scheduled));
        }

        [TestMethod]
        public void DeleteJob_RetrievesJobIdAndRemovesMessageWithErrorReason()
        {
            // Arrange
            const long expectedId = 4242L;
            var getJobId = Substitute.For<IQueryHandler<GetJobIdQuery<long>, long>>();
            getJobId.Handle(Arg.Any<GetJobIdQuery<long>>()).Returns(expectedId);
            var removeMessage = Substitute.For<IRemoveMessage>();

            var sut = CreateSut(getJobId: getJobId, removeMessage: removeMessage);

            // Act
            InvokeDeleteJob(sut, "jobToDelete");

            // Assert
            getJobId.Received(1).Handle(Arg.Is<GetJobIdQuery<long>>(q => q.JobName == "jobToDelete"));
            removeMessage.Received(1).Remove(
                Arg.Is<IMessageId>(id => id is MessageQueueId<long> && (long)((MessageQueueId<long>)id).Id.Value == expectedId),
                RemoveMessageReason.Error);
        }

        [TestMethod]
        public void Constructor_AssignsDependenciesWithoutThrowing()
        {
            // Arrange / Act
            var sut = CreateSut();

            // Assert
            Assert.IsNotNull(sut);
            Assert.IsInstanceOfType(sut, typeof(ASendJobToQueue));
        }

        #region Helpers

        private static SqlServerSendJobToQueue CreateSut(
            IProducerMethodQueue queue = null,
            IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses> doesJobExist = null,
            IRemoveMessage removeMessage = null,
            IQueryHandler<GetJobIdQuery<long>, long> getJobId = null,
            CreateJobMetaData createJobMetaData = null,
            IGetTimeFactory getTimeFactory = null)
        {
            return new SqlServerSendJobToQueue(
                queue ?? Substitute.For<IProducerMethodQueue>(),
                doesJobExist ?? Substitute.For<IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses>>(),
                removeMessage ?? Substitute.For<IRemoveMessage>(),
                getJobId ?? Substitute.For<IQueryHandler<GetJobIdQuery<long>, long>>(),
                createJobMetaData ?? new CreateJobMetaData(Substitute.For<IJobSchedulerMetaData>()),
                getTimeFactory ?? Substitute.For<IGetTimeFactory>());
        }

        private static QueueStatuses InvokeDoesJobExist(SqlServerSendJobToQueue sut, string name, DateTimeOffset scheduledTime)
        {
            var method = typeof(SqlServerSendJobToQueue).GetMethod("DoesJobExist",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (QueueStatuses)method.Invoke(sut, new object[] { name, scheduledTime });
        }

        private static void InvokeDeleteJob(SqlServerSendJobToQueue sut, string name)
        {
            var method = typeof(SqlServerSendJobToQueue).GetMethod("DeleteJob",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(sut, new object[] { name });
        }

        #endregion
    }
}
