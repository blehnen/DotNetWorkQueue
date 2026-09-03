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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    /// <summary>
    /// The in-memory store's de-queue, driven directly.
    /// </summary>
    [TestClass]
    public class DataStorageTests
    {
        [TestMethod]
        public void A_Message_That_Cannot_Be_Reassembled_Is_Poison()
        {
            //the de-queue has already taken the message off the queue by the time the body is
            //turned back into a message, so a failure there cannot be retried by putting it back -
            //it is reported as poison instead. Nothing covered this path.
            var messageFactory = Substitute.For<IMessageFactory>();
            messageFactory
                .When(x => x.Create(Arg.Any<object>(), Arg.Any<IDictionary<string, object>>()))
                .Do(_ => throw new InvalidOperationException("cannot re-assemble"));

            using var test = Create(messageFactory);
            var id = test.SendMessage(Message(), MessageData());
            Assert.AreNotEqual(Guid.Empty, id);

            var thrown = Assert.ThrowsExactly<PoisonMessageException>(
                () => test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));

            Assert.AreEqual(id, (Guid)thrown.MessageId.Id.Value);
        }

        [TestMethod]
        public void Disposing_After_A_De_Queue_Does_Not_Throw()
        {
            //the de-queue builds a cancellation source that lives as long as this object, so
            //disposal has to cope with it having been created
            var test = Create(MessageFactory());
            test.SendMessage(Message(), MessageData());
            Assert.IsNotNull(test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));

            test.Dispose();
            test.Dispose(); //disposal is guarded, so a second call is a no-op rather than a throw
        }

        [TestMethod]
        public void Disposing_Without_A_De_Queue_Does_Not_Throw()
        {
            //the cancellation source is never built in this case, so disposal must not force it
            var test = Create(MessageFactory());
            test.Dispose();
        }

        private static DataStorage Create(IMessageFactory messageFactory)
        {
            var jobSchedulerMetaData = Substitute.For<IJobSchedulerMetaData>();
            jobSchedulerMetaData.GetJobName(Arg.Any<IAdditionalMessageData>()).Returns(string.Empty);

            //a queue name of its own: the store is a process-wide static keyed by connection
            var connection = new DotNetWorkQueue.Transport.Memory.ConnectionInformation(
                new QueueConnection("dataStorageTests" + Guid.NewGuid().ToString("N"), "memory"));

            return new DataStorage(jobSchedulerMetaData, connection,
                Substitute.For<IReceivedMessageFactory>(), messageFactory,
                Substitute.For<IQueueCancelWork>());
        }

        private static IMessageFactory MessageFactory()
        {
            var messageFactory = Substitute.For<IMessageFactory>();
            messageFactory.Create(Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
                .Returns(_ => Substitute.For<IMessage>());
            return messageFactory;
        }

        private static IMessage Message()
        {
            var message = Substitute.For<IMessage>();
            message.Body.Returns("body");
            message.Headers.Returns(new Dictionary<string, object>());
            return message;
        }

        private static IAdditionalMessageData MessageData()
        {
            var data = Substitute.For<IAdditionalMessageData>();
            data.CorrelationId.Returns(new MessageCorrelationId(Guid.NewGuid()));
            return data;
        }
    }
}
