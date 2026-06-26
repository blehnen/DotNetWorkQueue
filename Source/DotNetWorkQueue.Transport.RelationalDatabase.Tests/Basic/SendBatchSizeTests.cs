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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class SendBatchSizeTests
    {
        [TestMethod]
        public void RequestedAboveSafeMax_ClampsDownToSafeMax()
        {
            var sut = new SendBatchSize(safeMaxBatchSize: 100, requestedBatchSize: 500);
            Assert.AreEqual(100, sut.BatchSize(1000));
        }

        [TestMethod]
        public void RequestedAtOrBelowSafeMax_IsHonored()
        {
            var sut = new SendBatchSize(safeMaxBatchSize: 100, requestedBatchSize: 50);
            Assert.AreEqual(50, sut.BatchSize(1000));
        }

        [TestMethod]
        public void NoRequestedCeiling_DefaultsToSafeMax()
        {
            var sut = new SendBatchSize(safeMaxBatchSize: 100);
            Assert.AreEqual(100, sut.BatchSize(1000));
        }

        [TestMethod]
        public void RequestedBelowOne_IsIgnored_DefaultsToSafeMax()
        {
            var sut = new SendBatchSize(safeMaxBatchSize: 100, requestedBatchSize: 0);
            Assert.AreEqual(100, sut.BatchSize(1000));
        }

        [TestMethod]
        public void MessageCountSmallerThanEffectiveMax_ReturnsMessageCount()
        {
            var sut = new SendBatchSize(safeMaxBatchSize: 100);
            Assert.AreEqual(30, sut.BatchSize(30));
        }

        [TestMethod]
        public void MessageCountBelowOne_ReturnsOne()
        {
            var sut = new SendBatchSize(safeMaxBatchSize: 100);
            Assert.AreEqual(1, sut.BatchSize(0));
        }

        [TestMethod]
        public void SafeMaxBelowOne_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SendBatchSize(0));
        }
    }
}
