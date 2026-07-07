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
using KellermanSoftware.CompareNetObjects;

namespace DotNetWorkQueue.Tests.Shared
{
    /// <summary>
    /// Shared MSTest assertion helpers covering patterns that FluentAssertions provided
    /// but MSTest has no clean 1:1 equivalent for: close-enough date/time comparisons,
    /// per-element collection assertions, and deep structural equivalence.
    /// </summary>
    public static class AssertHelper
    {
        /// <summary>
        /// Asserts that <paramref name="actual"/> is within <paramref name="tolerance"/> of
        /// <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">The expected date/time.</param>
        /// <param name="actual">The actual date/time.</param>
        /// <param name="tolerance">The maximum allowed difference between the two values.</param>
        public static void AreClose(DateTime expected, DateTime actual, TimeSpan tolerance)
        {
            var delta = actual - expected;
            if (Math.Abs(delta.Ticks) > tolerance.Ticks)
            {
                Assert.Fail($"Expected {actual:O} to be within {tolerance} of {expected:O}, but the difference was {delta}.");
            }
        }

        /// <summary>
        /// Asserts that <paramref name="actual"/> is within <paramref name="tolerance"/> of
        /// <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">The expected date/time offset.</param>
        /// <param name="actual">The actual date/time offset.</param>
        /// <param name="tolerance">The maximum allowed difference between the two values.</param>
        public static void AreClose(DateTimeOffset expected, DateTimeOffset actual, TimeSpan tolerance)
        {
            var delta = actual - expected;
            if (Math.Abs(delta.Ticks) > tolerance.Ticks)
            {
                Assert.Fail($"Expected {actual:O} to be within {tolerance} of {expected:O}, but the difference was {delta}.");
            }
        }

        /// <summary>
        /// Asserts that <paramref name="assertion"/> succeeds for every element of
        /// <paramref name="collection"/>. Stops and reports the first failing element.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="collection">The collection to check.</param>
        /// <param name="assertion">The assertion to run against each element.</param>
        public static void AllSatisfy<T>(IEnumerable<T> collection, Action<T> assertion)
        {
            ArgumentNullException.ThrowIfNull(collection);

            ArgumentNullException.ThrowIfNull(assertion);

            var index = 0;
            foreach (var item in collection)
            {
                try
                {
                    assertion(item);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"AllSatisfy failed for element at index {index} (value: {item}): {ex.Message}");
                }

                index++;
            }
        }

        /// <summary>
        /// Asserts that <paramref name="expected"/> and <paramref name="actual"/> are deeply,
        /// structurally equivalent.
        /// </summary>
        /// <typeparam name="T">The compared type.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="ignoreCollectionOrder">Whether collection members are compared without regard to order.</param>
        public static void AreEquivalent<T>(T expected, T actual, bool ignoreCollectionOrder = true)
        {
            var compareLogic = new CompareLogic(new ComparisonConfig { IgnoreCollectionOrder = ignoreCollectionOrder });
            var result = compareLogic.Compare(expected, actual);
            if (!result.AreEqual)
            {
                Assert.Fail(result.DifferencesString);
            }
        }
    }
}
