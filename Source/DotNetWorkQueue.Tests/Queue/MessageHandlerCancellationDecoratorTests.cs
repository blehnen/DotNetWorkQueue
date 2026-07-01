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
using System.Threading;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class MessageHandlerCancellationDecoratorTests
    {
        [TestMethod]
        public void Handle_Sets_MessageCancellation_On_WorkerNotification()
        {
            var tracker = new MessageCancellationTracker();
            var innerHandler = Substitute.For<IMessageHandler>();
            var decorator = new MessageHandlerCancellationDecorator(innerHandler, tracker, NullLogger.Instance);

            var message = CreateMessage("set-cancel-1");
            var notification = CreateNotification();

            IMessageCancellation captured = null;
            innerHandler.When(x => x.Handle(Arg.Any<IReceivedMessageInternal>(), Arg.Any<IWorkerNotification>()))
                .Do(ci => captured = ci.ArgAt<IWorkerNotification>(1).MessageCancellation);

            decorator.Handle(message, notification);

            Assert.IsNotNull(captured);
            Assert.IsTrue(captured.Token.CanBeCanceled);
        }

        [TestMethod]
        public void Handle_Unregisters_After_Success()
        {
            var tracker = new MessageCancellationTracker();
            var innerHandler = Substitute.For<IMessageHandler>();
            var decorator = new MessageHandlerCancellationDecorator(innerHandler, tracker, NullLogger.Instance);

            var message = CreateMessage("unregister-success-1");
            var notification = CreateNotification();

            decorator.Handle(message, notification);

            Assert.IsFalse(MessageCancellationTracker.IsProcessing("unregister-success-1"));
        }

        [TestMethod]
        public void Handle_Unregisters_After_Exception()
        {
            var tracker = new MessageCancellationTracker();
            var innerHandler = Substitute.For<IMessageHandler>();
            innerHandler.When(x => x.Handle(Arg.Any<IReceivedMessageInternal>(), Arg.Any<IWorkerNotification>()))
                .Do(_ => throw new InvalidOperationException("test"));

            var decorator = new MessageHandlerCancellationDecorator(innerHandler, tracker, NullLogger.Instance);
            var message = CreateMessage("unregister-error-1");
            var notification = CreateNotification();

            Action act = () => decorator.Handle(message, notification);
            Assert.Throws<InvalidOperationException>(act);

            Assert.IsFalse(MessageCancellationTracker.IsProcessing("unregister-error-1"));
        }

        [TestMethod]
        public void Handle_Clears_MessageCancellation_After_Complete()
        {
            var tracker = new MessageCancellationTracker();
            var innerHandler = Substitute.For<IMessageHandler>();
            var decorator = new MessageHandlerCancellationDecorator(innerHandler, tracker, NullLogger.Instance);

            var message = CreateMessage("clear-after-1");
            var notification = CreateNotification();

            decorator.Handle(message, notification);

            Assert.AreEqual(typeof(MessageCancellationNoOp), notification.MessageCancellation.GetType());
        }

        [TestMethod]
        public void Handle_Works_Without_MessageId()
        {
            var tracker = new MessageCancellationTracker();
            var innerHandler = Substitute.For<IMessageHandler>();
            var decorator = new MessageHandlerCancellationDecorator(innerHandler, tracker, NullLogger.Instance);

            var message = Substitute.For<IReceivedMessageInternal>();
            message.MessageId.Returns((IMessageId)null);
            var notification = CreateNotification();

            decorator.Handle(message, notification);

            innerHandler.Received(1).Handle(message, notification);
        }

        private static IReceivedMessageInternal CreateMessage(string queueId)
        {
            var message = Substitute.For<IReceivedMessageInternal>();
            var messageId = Substitute.For<IMessageId>();
            var setting = Substitute.For<ISetting>();
            setting.Value.Returns(queueId);
            messageId.Id.Returns(setting);
            messageId.HasValue.Returns(true);
            message.MessageId.Returns(messageId);
            return message;
        }

        private static IWorkerNotification CreateNotification()
        {
            var notification = Substitute.For<IWorkerNotification>();
            var cancelWork = Substitute.For<ICancelWork>();
            cancelWork.Tokens.Returns(new List<CancellationToken>());
            notification.WorkerStopping.Returns(cancelWork);
            notification.MessageCancellation = null;
            return notification;
        }
    }
}
