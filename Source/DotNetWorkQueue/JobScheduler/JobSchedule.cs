// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue.JobScheduler
{
    internal class JobSchedule: IJobSchedule
    {
        private readonly Schyntax.Schedule _schedule;

        public JobSchedule(string schedule, Func<DateTimeOffset> getCurrentOffset )
        {
            _schedule = new Schyntax.Schedule(schedule, getCurrentOffset);
        }
        public string OriginalText => _schedule.OriginalText;

        public DateTimeOffset Next()
        {
            return _schedule.Next();
        }

        public DateTimeOffset Next(DateTimeOffset after)
        {
            return _schedule.Next(after);
        }

        public DateTimeOffset Previous()
        {
            return _schedule.Previous();
        }

        public DateTimeOffset Previous(DateTimeOffset atOrBefore)
        {
            return _schedule.Previous(atOrBefore);
        }
    }
}
