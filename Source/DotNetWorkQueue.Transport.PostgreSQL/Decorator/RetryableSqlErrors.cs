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
