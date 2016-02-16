// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests
{
    public class QueueCreationResultTests
    {
        [Fact]
        public void Create_Ok()
        {
            var test = new QueueCreationResult(QueueCreationStatus.None, null);
            Assert.Equal(test.Status, QueueCreationStatus.None);
        }
        [Fact]
        public void GetSet_Status()
        {
            var test = new QueueCreationResult(QueueCreationStatus.AlreadyExists, null);
            Assert.Equal(QueueCreationStatus.AlreadyExists, test.Status);
        }
        [Theory, AutoData]
        public void GetSet_ErrorMessage(string message)
        {
            var test = new QueueCreationResult(QueueCreationStatus.AlreadyExists, message);
            Assert.Equal(message, test.ErrorMessage);
        }
        [Theory, AutoData]
        public void GetSet_ErrorMessage_True(string message)
        {
            var test = new QueueCreationResult(QueueCreationStatus.Success, message);
            Assert.True(test.Success);
        }
        [Theory, AutoData]
        public void GetSet_ErrorMessage_False(string message)
        {
            var test = new QueueCreationResult(QueueCreationStatus.ConfigurationError, message);
            Assert.False(test.Success);
        }
    }
}
