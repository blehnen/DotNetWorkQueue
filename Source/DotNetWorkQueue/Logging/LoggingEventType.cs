using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Logging event levels
    /// </summary>
    public enum LoggingEventType
    {
        /// <summary>
        /// trace
        /// </summary>
        Trace = 0,
        /// <summary>
        /// debug
        /// </summary>
        Debug = 1,
        /// <summary>
        /// information
        /// </summary>
        Information = 2,
        /// <summary>
        /// warning
        /// </summary>
        Warning = 3,
        /// <summary>
        /// error
        /// </summary>
        Error = 4 ,
        /// <summary>
        /// fatal
        /// </summary>
        Fatal = 5
    };
}
