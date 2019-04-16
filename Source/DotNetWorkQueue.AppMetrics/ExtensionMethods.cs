// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Linq;
using App.Metrics;

namespace DotNetWorkQueue.AppMetrics
{
    /// <summary>
    /// Extension methods for obtaining the current metrics
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the current metric values
        /// </summary>
        /// <param name="data">The data.</param>
        public static App.Metrics.MetricsDataValueSource GetCurrentMetrics(this App.Metrics.IMetrics data)
        {
            return data.Snapshot.Get();
        }
        /// <summary>
        /// Gets the current metrics.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static App.Metrics.MetricsDataValueSource GetCurrentMetrics(this Metrics data)
        {
            return (MetricsDataValueSource)data.CollectedMetrics;
        }

        public static App.Metrics.MetricTags GetTags(this List<KeyValuePair<string, string>> tags)
        {
            if (tags == null) return new MetricTags();
            return new App.Metrics.MetricTags(tags.Select(z => z.Key).ToArray(), tags.Select(z => z.Value).ToArray());
        }

        public static App.Metrics.Unit GetUnit(this Units unit)
        {
            switch (unit)
            {
                case Units.Bytes:
                    return Unit.Bytes;
                case Units.Calls:
                    return Unit.Calls;
                case Units.Commands:
                    return Unit.Commands;
                case Units.Errors:
                    return Unit.Errors;
                case Units.Events:
                    return Unit.Events;
                case Units.Items:
                    return Unit.Items;
                case Units.KiloBytes:
                    return Unit.KiloBytes;
                case Units.MegaBytes:
                    return Unit.MegaBytes;
                case Units.None:
                    return Unit.None;
                case Units.Percent:
                    return Unit.Percent;
                case Units.Requests:
                    return Unit.Requests;
                case Units.Results:
                    return Unit.Results;
                case Units.Threads:
                    return Unit.Threads;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }
    }
}
