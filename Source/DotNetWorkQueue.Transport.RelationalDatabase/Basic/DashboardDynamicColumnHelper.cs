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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Builds the dynamic column string for dashboard queries based on transport options.
    /// </summary>
    internal static class DashboardDynamicColumnHelper
    {
        /// <summary>
        /// Builds a comma-prefixed column list based on enabled transport options.
        /// Returns empty string if no optional columns are enabled.
        /// </summary>
        public static string BuildDynamicColumns(ITransportOptions opts)
        {
            var columns = string.Empty;
            if (opts.EnableStatus)
                columns += ", Status";
            if (opts.EnablePriority)
                columns += ", Priority";
            if (opts.EnableDelayedProcessing)
                columns += ", QueueProcessTime";
            if (opts.EnableHeartBeat)
                columns += ", HeartBeat";
            if (opts.EnableMessageExpiration)
                columns += ", ExpirationTime";
            if (opts.EnableRoute)
                columns += ", Route";
            return columns;
        }
    }
}
