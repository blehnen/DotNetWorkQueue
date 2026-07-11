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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Monitor that periodically purges old message history records.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "not needed")]
    public class ClearHistoryMonitor : BaseMonitor, IClearHistoryMonitor
    {
        /// <inheritdoc />
        public ClearHistoryMonitor(IBaseTransportOptions options,
            IPurgeMessageHistory purgeHistory, ILogger log)
            : base(CreatePurgeAction(options, Guard.NotNull(() => purgeHistory, purgeHistory)),
                  new MonitorTimespanWrapper(options.HistoryOptions?.MonitorTime ?? TimeSpan.FromDays(1)), log)
        {
        }

        private static Func<CancellationToken, long> CreatePurgeAction(
            IBaseTransportOptions options, IPurgeMessageHistory purgeHistory)
        {
            return token =>
            {
                var retentionDays = options.HistoryOptions?.RetentionDays ?? 30;
                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                return purgeHistory.Purge(cutoff);
            };
        }

        private sealed class MonitorTimespanWrapper : IMonitorTimespan
        {
            public MonitorTimespanWrapper(TimeSpan monitorTime) { MonitorTime = monitorTime; }
            public TimeSpan MonitorTime { get; set; }
        }
    }
}
