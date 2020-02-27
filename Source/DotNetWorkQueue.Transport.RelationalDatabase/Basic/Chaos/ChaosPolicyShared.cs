// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Chaos
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
        /// <param name="context">The context.</param>
        /// <param name="retryAttempts">The retry attempts.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static double InjectionRate(Context context, int retryAttempts, string keyName)
        {
            //check retry count;
            if (context.ContainsKey(keyName))
            {
                context.TryGetValue(keyName, out var value);
                context[keyName] = (int)context[keyName] + 1;
                if (value is int intValue1)
                {
                    if (intValue1 >= retryAttempts)
                        return 0; //no more errors, lets continue
                    return intValue1 == 0 ? 0.5 : 0.25;
                }
            }
            else
            {
                context.Add(keyName, 1);
            }
            return 0.5;
        }

        /// <summary>
        /// Returns the injection rate for a failure
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="retryAttempts">The retry attempts.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static async Task<double> InjectionRateAsync(Context context, int retryAttempts, string keyName)
        {
            return await RunAsync(() => InjectionRate(context, retryAttempts, keyName));
        }

        /// <summary>
        /// Runs a sync method async; used for libraries who demand an async function, but we don't have one
        /// </summary>
        /// <typeparam name="T">the output type</typeparam>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        public static Task<T> RunAsync<T>(Func<T> function)
        {
            Guard.NotNull(() => function, function);
            var tcs = new TaskCompletionSource<T>();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    T result = function();
                    tcs.SetResult(result);
                }
                catch (Exception exc) { tcs.SetException(exc); }
            });
            return tcs.Task;
        }
    }
}
