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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Tests.Shared;

namespace DotNetWorkQueue.Tests.Shared.Tests
{
    [TestClass]
    public class AssertHelperTests
    {
        #region AreClose (DateTime)

        [TestMethod]
        public void AreClose_DateTime_WithinTolerance_Passes()
        {
            var expected = new DateTime(2026, 1, 1, 12, 0, 0);
            var actual = expected.AddSeconds(2);
            var tolerance = TimeSpan.FromSeconds(5);

            AssertHelper.AreClose(expected, actual, tolerance);
        }

        [TestMethod]
        public void AreClose_DateTime_OutOfTolerance_Throws()
        {
            var expected = new DateTime(2026, 1, 1, 12, 0, 0);
            var actual = expected.AddSeconds(10);
            var tolerance = TimeSpan.FromSeconds(5);

            Assert.ThrowsExactly<AssertFailedException>(() => AssertHelper.AreClose(expected, actual, tolerance));
        }

        [TestMethod]
        public void AreClose_DateTime_DeltaExactlyEqualsTolerance_Passes()
        {
            var expected = new DateTime(2026, 1, 1, 12, 0, 0);
            var tolerance = TimeSpan.FromSeconds(5);
            var actual = expected.Add(tolerance);

            AssertHelper.AreClose(expected, actual, tolerance);
        }

        #endregion

        #region AreClose (DateTimeOffset)

        [TestMethod]
        public void AreClose_DateTimeOffset_WithinTolerance_Passes()
        {
            var expected = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var actual = expected.AddSeconds(2);
            var tolerance = TimeSpan.FromSeconds(5);

            AssertHelper.AreClose(expected, actual, tolerance);
        }

        [TestMethod]
        public void AreClose_DateTimeOffset_OutOfTolerance_Throws()
        {
            var expected = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var actual = expected.AddSeconds(10);
            var tolerance = TimeSpan.FromSeconds(5);

            Assert.ThrowsExactly<AssertFailedException>(() => AssertHelper.AreClose(expected, actual, tolerance));
        }

        [TestMethod]
        public void AreClose_DateTimeOffset_DeltaExactlyEqualsTolerance_Passes()
        {
            var expected = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            var tolerance = TimeSpan.FromSeconds(5);
            var actual = expected.Add(tolerance);

            AssertHelper.AreClose(expected, actual, tolerance);
        }

        #endregion

        #region AllSatisfy

        [TestMethod]
        public void AllSatisfy_AllElementsPass_DoesNotThrow()
        {
            var items = new List<int> { 2, 4, 6, 8 };

            AssertHelper.AllSatisfy(items, item => Assert.AreEqual(0, item % 2));
        }

        [TestMethod]
        public void AllSatisfy_OneElementFails_ThrowsAndReportsFailingIndex()
        {
            var items = new List<int> { 2, 4, 5, 8 };

            var exception = Assert.ThrowsExactly<AssertFailedException>(
                () => AssertHelper.AllSatisfy(items, item => Assert.AreEqual(0, item % 2)));

            StringAssert.Contains(exception.Message, "index");
            StringAssert.Contains(exception.Message, "2");
        }

        #endregion

        #region AreEquivalent

        [TestMethod]
        public void AreEquivalent_EqualObjects_DoesNotThrow()
        {
            var expected = new[] { "one", "two", "three" };
            var actual = new[] { "one", "two", "three" };

            AssertHelper.AreEquivalent(expected, actual);
        }

        [TestMethod]
        public void AreEquivalent_StringArray_OrderDifferent_DefaultIgnoresOrder_DoesNotThrow()
        {
            var expected = new[] { "one", "two", "three" };
            var actual = new[] { "three", "one", "two" };

            AssertHelper.AreEquivalent(expected, actual);
        }

        [TestMethod]
        public void AreEquivalent_ByteArray_Unequal_Throws()
        {
            var expected = new byte[] { 1, 2, 3 };
            var actual = new byte[] { 1, 2, 4 };

            Assert.ThrowsExactly<AssertFailedException>(() => AssertHelper.AreEquivalent(expected, actual));
        }

        [TestMethod]
        public void AreEquivalent_ByteArray_OrderDifferent_IgnoreCollectionOrderFalse_Throws()
        {
            var expected = new byte[] { 1, 2, 3 };
            var actual = new byte[] { 3, 2, 1 };

            Assert.ThrowsExactly<AssertFailedException>(
                () => AssertHelper.AreEquivalent(expected, actual, ignoreCollectionOrder: false));
        }

        #endregion
    }
}
