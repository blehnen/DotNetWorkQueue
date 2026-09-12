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
using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Configuration checks a consumer makes before it starts.
    /// </summary>
    internal static class ConsumerQueueConfigurationGuard
    {
        /// <summary>
        /// Rejects a heartbeat schedule that cannot survive a single missed beat.
        /// </summary>
        /// <remarks>
        /// A message whose stored heartbeat goes stale is reset by the monitor and handed to another
        /// worker, so the same message is processed twice. The original worker is only *asked* to stop -
        /// the token is advisory and handling code may ignore it - so the queue cannot make that
        /// harmless. Refusing a configuration with no room for one late beat is the lever it does have.
        /// </remarks>
        internal static void GuardHeartBeatSchedule(IHeartBeatConfiguration heartBeat)
        {
            if (heartBeat == null || !heartBeat.Enabled) return;

            if (heartBeat.UpdateTime <= TimeSpan.Zero)
                throw new DotNetWorkQueueException(
                    "HeartBeat.UpdateTime must be greater than zero when heartbeats are enabled; " +
                    "it is how often a worker renews its claim on the message it is processing.");

            if (heartBeat.Time <= TimeSpan.Zero)
                throw new DotNetWorkQueueException(
                    "HeartBeat.Time must be greater than zero when heartbeats are enabled; " +
                    "it is how long a worker's claim on a message remains valid.");

            //PeriodicTimer's range. Without this the consumer starts and the first message throws while
            //its heartbeat worker is being built, which is a much worse place to find out.
            if (heartBeat.UpdateTime < TimeSpan.FromMilliseconds(1) ||
                heartBeat.UpdateTime.TotalMilliseconds >= uint.MaxValue)
                throw new DotNetWorkQueueException(
                    $"HeartBeat.UpdateTime ({heartBeat.UpdateTime}) must be at least one millisecond and " +
                    $"less than {TimeSpan.FromMilliseconds(uint.MaxValue)}.");

            const int minimumAttempts = 3;
            var attempts = (int)(heartBeat.Time.Ticks / heartBeat.UpdateTime.Ticks);
            if (attempts >= minimumAttempts) return;

            throw new DotNetWorkQueueException(
                $"HeartBeat.Time ({heartBeat.Time}) must be at least {minimumAttempts} times " +
                $"HeartBeat.UpdateTime ({heartBeat.UpdateTime}), so that a beat can be missed without " +
                $"the message's claim expiring. With the current values there are {attempts} attempts " +
                "before the claim expires; a single delayed beat can cause the message to be reset and " +
                "processed a second time. Either raise HeartBeat.Time to " +
                $"{TimeSpan.FromTicks(heartBeat.UpdateTime.Ticks * minimumAttempts)}, or lower " +
                $"HeartBeat.UpdateTime to {TimeSpan.FromTicks(heartBeat.Time.Ticks / minimumAttempts)}");
        }
    }
}
