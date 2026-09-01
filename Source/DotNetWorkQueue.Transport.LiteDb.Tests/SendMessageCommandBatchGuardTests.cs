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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    /// <summary>
    /// The batch path writes every message in one transaction and never asks whether a job is
    /// already queued — that check is a check-then-act needing a lock the batch path deliberately
    /// does not take. A scheduled job in a batch therefore has to be refused rather than silently
    /// queued twice.
    /// </summary>
    [TestClass]
    public class SendMessageCommandBatchGuardTests
    {
        [TestMethod]
        public void An_Ordinary_Batch_Is_Allowed()
        {
            var metaData = Substitute.For<IJobSchedulerMetaData>();
            metaData.GetJobName(Arg.Any<IAdditionalMessageData>()).Returns((string)null);

            SendMessageCommandBatchShared.GuardNoScheduledJobs(Batch(3), metaData);
        }

        [TestMethod]
        [DataRow("", DisplayName = "empty job name is not a job")]
        [DataRow("   ", DisplayName = "blank job name is not a job")]
        public void A_Blank_Job_Name_Is_Not_A_Scheduled_Job(string jobName)
        {
            var metaData = Substitute.For<IJobSchedulerMetaData>();
            metaData.GetJobName(Arg.Any<IAdditionalMessageData>()).Returns(jobName);

            SendMessageCommandBatchShared.GuardNoScheduledJobs(Batch(3), metaData);
        }

        [TestMethod]
        public void A_Scheduled_Job_Anywhere_In_The_Batch_Is_Refused()
        {
            var messages = Batch(3);
            var metaData = Substitute.For<IJobSchedulerMetaData>();

            //only the last message is a job, so this also proves the whole batch is inspected
            metaData.GetJobName(Arg.Any<IAdditionalMessageData>())
                .Returns((string)null, (string)null, "a-scheduled-job");

            var ex = Assert.ThrowsExactly<NotSupportedException>(
                () => SendMessageCommandBatchShared.GuardNoScheduledJobs(messages, metaData));

            Assert.Contains("Send(message)", ex.Message,
                "the message should tell the caller how to send a scheduled job");
        }

        private static List<QueueMessage<IMessage, IAdditionalMessageData>> Batch(int count)
        {
            var messages = new List<QueueMessage<IMessage, IAdditionalMessageData>>(count);
            for (var i = 0; i < count; i++)
            {
                messages.Add(new QueueMessage<IMessage, IAdditionalMessageData>(
                    Substitute.For<IMessage>(), Substitute.For<IAdditionalMessageData>()));
            }
            return messages;
        }
    }
}
