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
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    /// <summary>
    /// The total a failed message's error count is set to.
    ///
    /// The count the transports store is now supplied by the caller rather than worked out by the
    /// statement, so the conversion from "what is stored" to "what to store next" happens here. The
    /// handler tests cannot see it - they are handed a command that already carries a number - so a
    /// mistake in this one addition would leave every retry decision wrong with nothing failing
    /// (GitHub #350).
    /// </summary>
    [TestClass]
    public class ReceiveErrorMessageRetryCountTests
    {
        [TestMethod]
        public void AFirstFailure_RecordsOne()
        {
            var h = new Harness(storedRetryCount: 0);

            h.Handler.MessageFailedProcessing(h.Message, h.Context, new InvalidOperationException());

            h.SetErrorCount.Received(1).Handle(
                Arg.Is<SetErrorCountCommand<long>>(c => c.RetryCount == 1));
        }

        [TestMethod]
        public void ALaterFailure_RecordsTheStoredCountPlusOne()
        {
            var h = new Harness(storedRetryCount: 2);

            h.Handler.MessageFailedProcessing(h.Message, h.Context, new InvalidOperationException());

            h.SetErrorCount.Received(1).Handle(
                Arg.Is<SetErrorCountCommand<long>>(c => c.RetryCount == 3));
        }

        [TestMethod]
        public async Task TheAsynchronousPath_RecordsTheSameTotal()
        {
            //the two paths are separate copies of the same few lines, so the addition can drift on one
            var h = new Harness(storedRetryCount: 2);

            await h.Handler.MessageFailedProcessingAsync(h.Message, h.Context, new InvalidOperationException());

            await h.SetErrorCountAsync.Received(1).HandleAsync(
                Arg.Is<SetErrorCountCommand<long>>(c => c.RetryCount == 3));
        }

        private sealed class Harness
        {
            public Harness(int storedRetryCount)
            {
                var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
                var configuration = fixture.Create<QueueConsumerConfiguration>();
                //Four attempts configured, so a stored count of two is still retryable. Stubbed rather
                //than added to the real behaviour: it is built by a factory, so what the configuration
                //holds here is a substitute whose Add does nothing and whose MaxRetries is zero - which
                //sends every message straight to the error queue and past the line under test.
                var retryInformation = Substitute.For<IRetryInformation>();
                retryInformation.ExceptionType.Returns(typeof(InvalidOperationException));
                retryInformation.MaxRetries.Returns(4);
                retryInformation.Times.Returns(new List<TimeSpan>
                {
                    TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)
                });
                configuration.TransportConfiguration.RetryDelayBehavior
                    .GetRetryAmount(Arg.Any<Exception>()).Returns(retryInformation);

                var query = Substitute.For<IQueryHandler<GetErrorRetryCountQuery<long>, int>>();
                query.Handle(Arg.Any<GetErrorRetryCountQuery<long>>()).Returns(storedRetryCount);
                var queryAsync = Substitute.For<IQueryHandlerAsync<GetErrorRetryCountQuery<long>, int>>();
                queryAsync.HandleAsync(Arg.Any<GetErrorRetryCountQuery<long>>())
                    .Returns(Task.FromResult(storedRetryCount));

                SetErrorCount = Substitute.For<ICommandHandler<SetErrorCountCommand<long>>>();
                SetErrorCountAsync = Substitute.For<ICommandHandlerAsync<SetErrorCountCommand<long>>>();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(true);
                var id = Substitute.For<ISetting>();
                id.Value.Returns(42L);
                messageId.Id.Returns(id);

                Context = Substitute.For<IMessageContext>();
                Context.MessageId.Returns(messageId);

                Message = Substitute.For<IReceivedMessageInternal>();

                Handler = new ReceiveErrorMessage<long>(configuration, query, queryAsync,
                    SetErrorCount, SetErrorCountAsync,
                    Substitute.For<ICommandHandler<MoveRecordToErrorQueueCommand<long>>>(),
                    Substitute.For<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>>(),
                    Substitute.For<ILogger>(),
                    Substitute.For<IIncreaseQueueDelay>());
            }

            public ReceiveErrorMessage<long> Handler { get; }
            public ICommandHandler<SetErrorCountCommand<long>> SetErrorCount { get; }
            public ICommandHandlerAsync<SetErrorCountCommand<long>> SetErrorCountAsync { get; }
            public IMessageContext Context { get; }
            public IReceivedMessageInternal Message { get; }
        }
    }
}
