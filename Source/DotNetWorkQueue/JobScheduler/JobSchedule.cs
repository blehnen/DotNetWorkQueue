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
using Cronos;
using CronExpressionDescriptor;

namespace DotNetWorkQueue.JobScheduler
{
    internal class JobSchedule : IJobSchedule
    {
        private static readonly TimeSpan DefaultLookbackWindow = TimeSpan.FromHours(48);

        private readonly CronExpression _expression;
        private readonly string _originalText;
        private readonly Func<DateTimeOffset> _getCurrentOffset;
        private readonly Lazy<string> _description;
        private readonly TimeSpan _previousLookbackWindow;

        public JobSchedule(string schedule, Func<DateTimeOffset> getCurrentOffset, TimeSpan previousLookbackWindow = default)
        {
            _originalText = schedule;
            _getCurrentOffset = getCurrentOffset;
            _previousLookbackWindow = previousLookbackWindow > TimeSpan.Zero ? previousLookbackWindow : DefaultLookbackWindow;

            var fieldCount = schedule.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var format = fieldCount switch
            {
                5 => CronFormat.Standard,
                6 => CronFormat.IncludeSeconds,
                _ => throw new ArgumentException(
                    $"Cron expression must have 5 or 6 fields, but got {fieldCount}: '{schedule}'",
                    nameof(schedule))
            };

            _expression = CronExpression.Parse(schedule, format);
            _description = new Lazy<string>(() => ExpressionDescriptor.GetDescription(schedule));
        }

        public string OriginalText => _originalText;

        public string Description => _description.Value;

        public DateTimeOffset Next()
        {
            var next = _expression.GetNextOccurrence(_getCurrentOffset(), TimeZoneInfo.Utc);
            if (next == null)
                throw new InvalidOperationException(
                    $"No next occurrence found for cron expression: {_originalText}");
            return next.Value;
        }

        public DateTimeOffset Next(DateTimeOffset after)
        {
            var next = _expression.GetNextOccurrence(after, TimeZoneInfo.Utc);
            if (next == null)
                throw new InvalidOperationException(
                    $"No next occurrence found for cron expression: {_originalText}");
            return next.Value;
        }

        public DateTimeOffset? Previous()
        {
            return PreviousInternal(_getCurrentOffset());
        }

        public DateTimeOffset? Previous(DateTimeOffset atOrBefore)
        {
            return PreviousInternal(atOrBefore);
        }

        private DateTimeOffset? PreviousInternal(DateTimeOffset before)
        {
            var from = before - _previousLookbackWindow;
            var occurrences = _expression.GetOccurrences(
                from.UtcDateTime,
                before.UtcDateTime,
                TimeZoneInfo.Utc,
                fromInclusive: true,
                toInclusive: true);

            DateTimeOffset? last = null;
            foreach (var occurrence in occurrences)
            {
                last = new DateTimeOffset(occurrence, TimeSpan.Zero);
            }
            return last;
        }
    }
}
