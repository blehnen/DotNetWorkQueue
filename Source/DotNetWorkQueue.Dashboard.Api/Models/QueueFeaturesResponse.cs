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
namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Response model for enabled transport features.
    /// </summary>
    public class QueueFeaturesResponse
    {
        /// <summary>Gets or sets whether priority is enabled.</summary>
        public bool EnablePriority { get; set; }

        /// <summary>Gets or sets whether status tracking is enabled.</summary>
        public bool EnableStatus { get; set; }

        /// <summary>Gets or sets whether the status table is enabled.</summary>
        public bool EnableStatusTable { get; set; }

        /// <summary>Gets or sets whether heartbeat monitoring is enabled.</summary>
        public bool EnableHeartBeat { get; set; }

        /// <summary>Gets or sets whether delayed processing is enabled.</summary>
        public bool EnableDelayedProcessing { get; set; }

        /// <summary>Gets or sets whether message expiration is enabled.</summary>
        public bool EnableMessageExpiration { get; set; }

        /// <summary>Gets or sets whether message routing is enabled.</summary>
        public bool EnableRoute { get; set; }
    }
}
