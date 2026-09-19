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
            var enableAes = interceptorOptions.Aes is { Enabled: true };

            if (!enableGZip && !enableAes)
                return null;

            ValidateInterceptorOptions(enableAes, interceptorOptions);

            // Capture values for the closure
            var gzipOptions = interceptorOptions.GZip;
            var aesOptions = interceptorOptions.Aes;

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


                if (enableAes)
                {
                    types.Add(typeof(AesMessageInterceptor));
                    container.Register(() =>
                        new AesMessageInterceptorConfiguration(Convert.FromBase64String(aesOptions.Key)),
                        LifeStyles.Singleton);
                }

                container.RegisterCollection<IMessageInterceptor>(types);
            };
        }

        /// <summary>
        /// Validates the interceptor options up front, throwing <see cref="InvalidOperationException"/>
        /// for any misconfiguration so all validation failures share one exception type.
        /// </summary>
        private static void ValidateInterceptorOptions(bool enableAes, DashboardInterceptorOptions interceptorOptions)
        {
            if (enableAes)
            {
                if (string.IsNullOrEmpty(interceptorOptions.Aes.Key))
                    throw new InvalidOperationException("Aes interceptor requires a Key (Base64-encoded).");

                byte[] decodedAesKey;
                try
                {
                    decodedAesKey = Convert.FromBase64String(interceptorOptions.Aes.Key);
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException("Aes interceptor Key is not a valid Base64 string.", ex);
                }

                if (decodedAesKey.Length != 32)
                    throw new InvalidOperationException("Aes interceptor Key must decode to 32 bytes (AES-256).");
            }
        }
    }
}
