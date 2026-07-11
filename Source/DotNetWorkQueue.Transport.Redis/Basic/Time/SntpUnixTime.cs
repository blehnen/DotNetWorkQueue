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
using System.Threading;
using DotNetWorkQueue.Logging;
using GuerrillaNtp;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Returns the current unix time, using an NTP server
    /// </summary>
    internal class SntpUnixTime : BaseUnixTime
    {
        private readonly object _getTime = new object();
        private long _millisecondsDifference;
        private readonly NtpClient _ntpClient;
        private readonly ResiliencePipeline _queryPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="SntpUnixTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        public SntpUnixTime(ILogger log, SntpTimeConfiguration configuration)
            : base(log, configuration)
        {
            _ntpClient = new NtpClient(configuration.Server, configuration.TimeOut, configuration.Port);

            //a single UDP query to a public NTP pool can be silently dropped or rate-limited, surfacing
            //as a socket timeout. Retry a few times with jittered backoff so a transient loss doesn't fail.
            _queryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(250),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        Log.LogWarning(args.Outcome.Exception, "NTP query failed; retrying in {RetryDelayMs} ms (attempt {AttemptNumber})", args.RetryDelay.TotalMilliseconds, args.AttemptNumber + 1);
                        return default;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Gets the name of the time provider
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "SNTP";

        /// <summary>
        /// Gets the time from the server as needed, based on cache expiration
        /// </summary>
        /// <returns></returns>
        protected override long GetUnixTime()
        {
            if (!TimeExpired()) return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;

            //Serialize refreshes so only one thread queries the NTP server at a time - a burst of
            //concurrent queries is exactly what public pools rate-limit. But other threads must not
            //block behind the (retry-wrapped, so potentially multi-second on an outage) query: on a
            //warm cache they serve the last-known offset instead. Only the very first (cold) call,
            //which has no cached value yet, waits for the query to complete.
            var lockTaken = false;
            try
            {
                if (ServerOffsetObtained == default)
                    Monitor.Enter(_getTime, ref lockTaken);
                else
                    Monitor.TryEnter(_getTime, ref lockTaken);

                if (lockTaken && TimeExpired())
                {
                    var clock = _queryPipeline.Execute(() => _ntpClient.Query());
                    _millisecondsDifference = (long)clock.CorrectionOffset.TotalMilliseconds;
                    ServerOffsetObtained = DateTime.UtcNow;
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_getTime);
            }
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;
        }
    }
}
