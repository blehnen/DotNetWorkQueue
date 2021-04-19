// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    internal static class RetryablePostGreErrors
    {
        public static IEnumerable<string> Errors
        {
            get
            {
                yield return "53000"; //insufficient_resources
                yield return "53100"; //disk_full
                yield return "53200"; //out_of_memory
                yield return "53300"; //too_many_connections
                yield return "53400"; //configuration_limit_exceeded
                yield return "57P03"; //cannot_connect_now
                yield return "58000"; //system_error
                yield return "58030"; //io_error
                yield return "40001"; //serialization_error
                yield return "55P03"; //lock_not_available
                yield return "55006"; //object_in_use
                yield return "55000"; //object_not_in_prerequisite_state
                yield return "08000"; //connection_exception
                yield return "08003"; //connection_does_not_exist
                yield return "08006"; //connection_failure
                yield return "08001"; //client_unable_to_establish_connection
                yield return "08004"; //server_rejected_establishment_of_connection
                yield return "08007"; //transaction_resolution_unknown
            }
        }
    }
}
