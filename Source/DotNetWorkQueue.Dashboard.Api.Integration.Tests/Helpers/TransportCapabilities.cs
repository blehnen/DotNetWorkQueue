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
namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    public class TransportCapabilities
    {
        public bool HasErrors { get; init; }
        public bool HasStaleMessages { get; init; }
        public bool HasConfiguration { get; init; }
        public bool HasJobs { get; init; }
        public bool HasWriteOperations { get; init; }
        public bool HasEditBody { get; init; }

        public static TransportCapabilities SqlServer => new()
        {
            HasErrors = true,
            HasStaleMessages = true,
            HasConfiguration = true,
            HasJobs = true,
            HasWriteOperations = true,
            HasEditBody = true
        };

        public static TransportCapabilities PostgreSql => new()
        {
            HasErrors = true,
            HasStaleMessages = true,
            HasConfiguration = true,
            HasJobs = true,
            HasWriteOperations = true,
            HasEditBody = true
        };

        public static TransportCapabilities Sqlite => new()
        {
            HasErrors = true,
            HasStaleMessages = true,
            HasConfiguration = true,
            HasJobs = true,
            HasWriteOperations = true,
            HasEditBody = true
        };

        public static TransportCapabilities Redis => new()
        {
            HasErrors = true,
            HasStaleMessages = true,
            HasConfiguration = false,
            HasJobs = false,
            HasWriteOperations = true,
            HasEditBody = false
        };

        public static TransportCapabilities LiteDb => new()
        {
            HasErrors = true,
            HasStaleMessages = true,
            HasConfiguration = false,
            HasJobs = false,
            HasWriteOperations = true,
            HasEditBody = false
        };

        public static TransportCapabilities Memory => new()
        {
            HasErrors = false,
            HasStaleMessages = false,
            HasConfiguration = false,
            HasJobs = false,
            HasWriteOperations = true,
            HasEditBody = false
        };
    }
}
