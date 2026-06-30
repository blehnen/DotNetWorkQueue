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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Pins the chunk-size selection the PostgreSQL batch handler relies on: a configured
    /// <c>BatchSize</c> ceiling is honored when below the transport safe-max and clamped to the
    /// safe-max when above it. (The handler builds exactly this <see cref="SendBatchSize"/> from
    /// <see cref="SendMessageBatch.SafeMaxBatchSize"/> and the option; the integration suite then
    /// proves end-to-end delivery on the configured path.)
    /// </summary>
    [TestClass]
    public class BatchSizeSelectionTests
    {
        [TestMethod]
        public void SafeMax_IsPositive()
        {
            Assert.IsTrue(SendMessageBatch.SafeMaxBatchSize >= 1);
        }

        [TestMethod]
        public void ConfiguredBatchSize_BelowSafeMax_IsHonored()
        {
            var sizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize, 50);
            Assert.AreEqual(50, sizer.BatchSize(1000));
        }

        [TestMethod]
        public void ConfiguredBatchSize_AboveSafeMax_IsClampedToSafeMax()
        {
            var sizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize, SendMessageBatch.SafeMaxBatchSize + 10_000);
            Assert.AreEqual(SendMessageBatch.SafeMaxBatchSize, sizer.BatchSize(1_000_000));
        }

        [TestMethod]
        public void NoConfiguredBatchSize_UsesSafeMax()
        {
            var sizer = new SendBatchSize(SendMessageBatch.SafeMaxBatchSize, null);
            Assert.AreEqual(SendMessageBatch.SafeMaxBatchSize, sizer.BatchSize(1_000_000));
        }
    }
}
