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
    /// Receives notifications when consumer message processing events occur.
    /// Implement this interface to track consumer metrics (e.g. in a dashboard).
    /// </summary>
    public interface IConsumerMetricsNotification
    {
        /// <summary>Called when a message has been successfully processed.</summary>
        void IncrementProcessed();

        /// <summary>Called when a message processing error occurs.</summary>
        void IncrementErrored();

        /// <summary>Called when a message is rolled back for re-processing.</summary>
        void IncrementRolledBack();

        /// <summary>Called when a poison message is detected.</summary>
        void IncrementPoisonMessage();
    }
}
