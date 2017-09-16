// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageMethodHandlingTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Disposed_Instance_HandleExecution_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new MessageExpression());
            var test = Create();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.HandleExecution(new ReceivedMessage<MessageExpression>(message), new WorkerNotificationNoOp());
                });
        }


        [Fact]
        public void Calling_HandleExecution_Null_Exception()
        {
            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(null, null);
                    });
            }
        }

        [Fact]
        public void Calling_HandleExecution_Null_Exception2()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new MessageExpression());

            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(
                            new ReceivedMessage<MessageExpression>(message), null);
                    });
            }
        }

        [Fact]
        public void Calling_HandleExecution_Null_Exception3()
        {
            using (var test = Create())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.HandleExecution(
                            null, new WorkerNotificationNoOp());
                    });
            }
        }

        private IMessageMethodHandling Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageMethodHandling>();
        }
    }
}
