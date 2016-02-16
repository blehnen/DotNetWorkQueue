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
using System;
using System.Linq;
using DotNetWorkQueue.QueueStatus;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueInformationErrorTests
    {
        [Theory, AutoData]
        public void Create(string name, string server, Exception error)
        {
            IQueueInformation test = new QueueInformationError(name, server, error);
            Assert.Equal(name, test.Name);
            Assert.Equal(server, test.Server);
            Assert.Equal(DateTime.MinValue, test.CurentDateTime);
            Assert.Equal(string.Empty, test.DateTimeProvider);
            Assert.Equal(1, test.Data.Count());
            var systemEntries = test.Data.ToList();
            Assert.Contains(error.ToString(), systemEntries[0].Value);
            Assert.Contains("Error", systemEntries[0].Name);
        }
    }
}
