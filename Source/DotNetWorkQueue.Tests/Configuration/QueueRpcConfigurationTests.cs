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

using DotNetWorkQueue.Configuration;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueRpcConfigurationTests
    {
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
        }

        [Fact]
        public void Set_Readonly_SetsTransportConfigurationReceive()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.TransportConfigurationReceive.IsReadOnly);
        }

        [Fact]
        public void Set_Readonly_SetsMessageExpiration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            configuration.MessageExpiration.Received(1).SetReadOnly();
        }

        [Fact]
        public void Set_Readonly_SetsTransportConfigurationSend()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.TransportConfigurationSend.IsReadOnly);
        }

        private QueueRpcConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueRpcConfiguration>();
        }
    }
}
