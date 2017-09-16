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
using System.Linq;
using DotNetWorkQueue.Messages;
using FluentAssertions;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageContextTests
    {
        [Theory, AutoData]
        public void GetSet_AdditionalContextData(string name)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var test = fixture.Create<MessageContext>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(name, headerData);
            test.Set(property, headerData);

            var headerData2 = test.Get(property);
            Assert.Equal(headerData2, headerData);
        }
        [Theory, AutoData]
        public void GetSet_AdditionalContextData_Default_Value(string name)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            messageContextDataFactory.Create(name, headerData).Returns(new MessageContextData<HeaderData>(name, headerData));

            var property = messageContextDataFactory.Create(name, headerData);
            var headerData2 = test.Get(property);
            Assert.Equal(headerData2, headerData);

            var headerData3 = test.Get(property);
            Assert.Equal(headerData2, headerData3);
        }

        [Fact]
        public void Commit_Fires()
        {
            const string value = "Commit";
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.MonitorEvents();
            test.RaiseCommit();
            test.ShouldRaise(value);
        }

        [Fact]
        public void Rollback_Fires()
        {
            const string value = "Rollback";
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.MonitorEvents();
            test.RaiseRollback();
            test.ShouldRaise(value);
        }


        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Cleanup_Fires_Only_Once()
        {
            const string value = "Cleanup";
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.MonitorEvents();
            test.Dispose();
            test.Dispose();
            var list = test.ShouldRaise(value).ToList();
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void WorkerNotification_NotNull()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            Assert.NotNull(test.WorkerNotification);
        }

        [Fact]
        public void Cleanup_Fires()
        {
            const string value = "Cleanup";
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.MonitorEvents();
            test.Dispose();
            test.ShouldRaise(value);
        }

        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            Assert.False(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Commit_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.RaiseCommit();
            });
        }
        [Fact]
        public void Disposed_Instance_Rollback_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.RaiseRollback();
            });
        }
        [Theory, AutoData]
        public void Disposed_Instance_SetAdditionalContextData_Exception(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            var messageContextDataFactory =
              fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(value, headerData);
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Set(property, headerData);
            });
        }
        [Theory, AutoData]
        public void Disposed_Instance_GetAdditionalContextData_Exception(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            var messageContextDataFactory =
              fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(value, headerData);
            test.Set(property, headerData);
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Get(property);
            });
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public class HeaderData : IDisposable
        {
            [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
            public void Dispose()
            {
                
            }
        }
    }
}
