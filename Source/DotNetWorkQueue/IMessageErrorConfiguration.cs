using System;
namespace DotNetWorkQueue
{
    public interface IMessageErrorConfiguration : IMonitorTimespan, IReadonly, ISetReadonly
    {
        /// <summary>
        /// If true, the queue will check for and delete messages that have an error status
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Messages that are older than this value, and are in an error status, will be deleted.
        /// </summary>
        /// <remarks>Age is based on the error time stamp, not the date the message was created</remarks>
        TimeSpan MessageAge { get; set; }
    }
}
