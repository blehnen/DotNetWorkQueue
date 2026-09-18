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

namespace DotNetWorkQueue
{
    /// <summary>
    /// The outcome of a schema upgrade.
    /// </summary>
    public class SchemaUpgradeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaUpgradeResult"/> class.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="startingVersion">The version the queue was at before the upgrade.</param>
        /// <param name="endingVersion">The version the queue is at after the upgrade.</param>
        /// <param name="errorMessage">The error message, if the upgrade failed.</param>
        public SchemaUpgradeResult(SchemaUpgradeStatus status, long startingVersion, long endingVersion,
            string errorMessage = null)
        {
            Status = status;
            StartingVersion = startingVersion;
            EndingVersion = endingVersion;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// The status of the upgrade.
        /// </summary>
        public SchemaUpgradeStatus Status { get; }

        /// <summary>
        /// The version the queue was at before the upgrade ran.
        /// </summary>
        public long StartingVersion { get; }

        /// <summary>
        /// The version the queue is at now.
        /// </summary>
        public long EndingVersion { get; }

        /// <summary>
        /// The error message, if the upgrade failed. Null otherwise.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// True if the schema is now at the version the library expects.
        /// </summary>
        /// <remarks>
        /// True for <see cref="SchemaUpgradeStatus.AlreadyCurrent"/> as well as
        /// <see cref="SchemaUpgradeStatus.Upgraded"/>: both leave the queue usable, which is what a
        /// caller deciding whether to carry on actually needs to know.
        /// </remarks>
        public bool Success => Status == SchemaUpgradeStatus.Upgraded ||
                               Status == SchemaUpgradeStatus.AlreadyCurrent;
    }

    /// <summary>
    /// The status of a schema upgrade.
    /// </summary>
    public enum SchemaUpgradeStatus
    {
        /// <summary>
        /// Default status.
        /// </summary>
        /// <remarks>Returning this would indicate a logic error; it is never a real outcome.</remarks>
        None = 0,

        /// <summary>
        /// The schema was upgraded.
        /// </summary>
        Upgraded = 1,

        /// <summary>
        /// The schema was already at the version the library expects; nothing was changed.
        /// </summary>
        /// <remarks>
        /// Also what a caller gets when another process upgraded the queue while this one waited for
        /// the lock. That is a success, not a conflict - the queue is at the version asked for.
        /// </remarks>
        AlreadyCurrent = 2,

        /// <summary>
        /// The queue does not exist, so there was nothing to upgrade.
        /// </summary>
        QueueDoesNotExist = 3,

        /// <summary>
        /// The upgrade failed. See <see cref="SchemaUpgradeResult.ErrorMessage"/>.
        /// </summary>
        /// <remarks>The schema is unchanged: an upgrade applies completely or not at all.</remarks>
        Failed = 4
    }
}
