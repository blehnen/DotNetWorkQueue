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
using DotNetWorkQueue.Interceptors;

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// Builds an <see cref="Action{IContainer}"/> from <see cref="DashboardInterceptorOptions"/>
    /// or named interceptor profiles.
    /// </summary>
    internal static class InterceptorConfigurationBuilder
    {
        /// <summary>
        /// Resolves the effective interceptor configuration for a queue, checking
        /// (in priority order): explicit delegate, named profile, JSON options.
        /// </summary>
        /// <param name="queueOptions">The queue options.</param>
        /// <param name="profiles">The registered interceptor profiles.</param>
        /// <returns>An action to configure the container, or null if no interceptors are configured.</returns>
        public static Action<IContainer> Resolve(
            DashboardQueueOptions queueOptions,
            IReadOnlyDictionary<string, Action<IContainer>> profiles)
        {
            // 1. Explicit delegate takes highest priority (existing code-based API)
            if (queueOptions.InterceptorConfiguration != null)
                return queueOptions.InterceptorConfiguration;

            // 2. Named profile
            if (!string.IsNullOrEmpty(queueOptions.InterceptorProfile))
            {
                if (profiles.TryGetValue(queueOptions.InterceptorProfile, out var profileAction))
                    return profileAction;

                throw new InvalidOperationException(
                    $"Interceptor profile '{queueOptions.InterceptorProfile}' is not registered. " +
                    $"Call options.AddInterceptorProfile(\"{queueOptions.InterceptorProfile}\", ...) during startup.");
            }

            // 3. JSON-bindable options for built-in interceptors
            if (queueOptions.Interceptors != null)
                return BuildFromOptions(queueOptions.Interceptors);

            return null;
        }

        /// <summary>
        /// Builds an <see cref="Action{IContainer}"/> from JSON-bindable interceptor options.
        /// </summary>
        private static Action<IContainer> BuildFromOptions(DashboardInterceptorOptions interceptorOptions)
        {
            var enableGZip = interceptorOptions.GZip is { Enabled: true };
            var enableTripleDes = interceptorOptions.TripleDes is { Enabled: true };

            if (!enableGZip && !enableTripleDes)
                return null;

            // Validate TripleDES config upfront
            if (enableTripleDes)
            {
                if (string.IsNullOrEmpty(interceptorOptions.TripleDes.Key))
                    throw new InvalidOperationException("TripleDes interceptor requires a Key (Base64-encoded).");
                if (string.IsNullOrEmpty(interceptorOptions.TripleDes.IV))
                    throw new InvalidOperationException("TripleDes interceptor requires an IV (Base64-encoded).");
            }

            // Capture values for the closure
            var gzipOptions = interceptorOptions.GZip;
            var tripleDesOptions = interceptorOptions.TripleDes;

            return container =>
            {
                var types = new List<Type>();

                if (enableGZip)
                {
                    types.Add(typeof(GZipMessageInterceptor));
                    container.Register(() =>
                        new GZipMessageInterceptorConfiguration { MinimumSize = gzipOptions.MinimumSize },
                        LifeStyles.Singleton);
                }

                if (enableTripleDes)
                {
                    types.Add(typeof(TripleDesMessageInterceptor));
                    container.Register(() =>
                        new TripleDesMessageInterceptorConfiguration(
                            Convert.FromBase64String(tripleDesOptions.Key),
                            Convert.FromBase64String(tripleDesOptions.IV)),
                        LifeStyles.Singleton);
                }

                container.RegisterCollection<IMessageInterceptor>(types);
            };
        }
    }
}
