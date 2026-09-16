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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Carries the heartbeat a de-queue stamped on a message, from the transport that wrote it to the
    /// worker that has to defend the claim it represents.
    /// </summary>
    /// <remarks>
    /// A claim is held by a heartbeat value, and that value is written twice by two different parties:
    /// the de-queue stamps it, and every later beat replaces it. Without this the worker only ever knew
    /// about the second one, so between being handed a message and its own first beat landing it held a
    /// claim whose value it could not name - and both places that prove ownership work by naming it.
    ///
    /// The beat compares it (<c>and (@previous is null or HeartBeat = @previous)</c>) and the rollback
    /// compares it (<c>AND heartbeat = @HeartBeat</c>), so with the value unknown the first had to give
    /// up the claim rather than renew it, and the second dropped its comparison altogether and reset the
    /// row unconditionally - including a row a second worker now owns.
    ///
    /// This travels on the message context rather than in the message headers, because it describes this
    /// delivery rather than the message: a redelivery is a different claim with a different value, and
    /// nothing about it should survive into the stored message (GitHub #336).
    /// </remarks>
    public interface IMessageClaim
    {
        /// <summary>
        /// The heartbeat the de-queue wrote, as the transport wrote it.
        /// </summary>
        /// <remarks>
        /// Absent when the transport does not stamp one - a de-queue that deletes the record rather than
        /// marking it, or a queue with the heartbeat option off. Absent is not an error; it means the
        /// same thing it meant before this existed, and the worker falls back to refusing to send a beat
        /// it cannot prove once the claim is past its expiry.
        ///
        /// Written by the transport's own clock, which on SQL Server is the database's rather than the
        /// worker's. It is only ever compared against itself, never against a local time, so that
        /// difference does not matter here - and it is exactly why seeding this from the worker's clock
        /// instead was rejected.
        /// </remarks>
        IMessageContextData<ValueTypeWrapper<DateTime>> ClaimedAt { get; }
    }
}
