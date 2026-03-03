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
using Polly;

namespace DotNetWorkQueue.Transport.Shared.Basic.Chaos
{
    /// <summary>
    /// Shared chaos policy helper functions
    /// </summary>
    public static class ChaosPolicyShared
    {
        /// <summary>
        /// Gets a random enum value
        /// </summary>
        /// <typeparam name="T">the enum</typeparam>
        /// <returns>a random selection from the enum</returns>
        public static T GetRandomEnum<T>()
            where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(ThreadSafeRandom.Next(values.Length));
        }

        /// <summary>
        /// Gets a random string.
        /// </summary>
        /// <param name="input">The input list of strings</param>
        /// <returns>a random selection</returns>
        public static string GetRandomString(List<string> input)
        {
            return input[ThreadSafeRandom.Next(input.Count)];
        }

        /// <summary>
        /// Returns the injection rate for a failure
        /// </summary>
        /// <param name="context">The resilience context.</param>
        /// <param name="retryAttempts">The retry attempts.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static double InjectionRate(ResilienceContext context, int retryAttempts, string keyName)
        {
            var key = new ResiliencePropertyKey<int>(keyName);
            if (context.Properties.TryGetValue(key, out var value))
            {
                context.Properties.Set(key, value + 1);
                if (value >= retryAttempts)
                    return 0; //no more errors, lets continue
                return value == 0 ? 0.5 : 0.25;
            }
            else
            {
                context.Properties.Set(key, 1);
            }
            return 0.5;
        }
    }
}
