// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageExceptionHandlerTests
    {
        [Fact]
        public void Message_Handled()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var error = fixture.Create<IReceiveMessagesError>();
            fixture.Inject(error);
            var test = fixture.Create<MessageExceptionHandler>();

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            Assert.Throws<MessageException>(
           delegate
           {
               test.Handle(message, context, exception);
           });

            error.Received(1).MessageFailedProcessing(message, context, exception);
        }
        [Fact]
        public void Message_Handled_Exception_Throws_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IReceiveMessagesError error = fixture.Create<ReceiveMessagesErrorWillCrash>();
            fixture.Inject(error);
            var test = new MessageExceptionHandler(error, Substitute.For<ILogFactory>());

            var message = fixture.Create<IReceivedMessageInternal>();
            var context = fixture.Create<IMessageContext>();
            var exception = new Exception();

            Assert.Throws<DotNetWorkQueueException>(
            delegate
            {
                test.Handle(message, context, exception);
            });
        }
        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReceiveMessagesErrorWillCrash: IReceiveMessagesError 
        {
            public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
            {
                throw new NotImplementedException();
            }
        }
    }
}
