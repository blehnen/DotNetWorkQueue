using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface ITransportOptions
    {
        #region Options

        /// <summary>
        /// Gets or sets a value indicating whether [enable priority].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable priority]; otherwise, <c>false</c>.
        /// </value>
        bool EnablePriority
        {
            get;
            set;
        }
        /// <summary>
        /// If true, a transaction will be held until the message is finished processing.
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable hold transaction until message committed]; otherwise, <c>false</c>.
        /// </value>
        bool EnableHoldTransactionUntilMessageCommited
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets a value indicating whether [enable status].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status]; otherwise, <c>false</c>.
        /// </value>
        bool EnableStatus
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets a value indicating whether [enable heart beat].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable heart beat]; otherwise, <c>false</c>.
        /// </value>
        bool EnableHeartBeat
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets a value indicating whether [enable delayed processing].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable delayed processing]; otherwise, <c>false</c>.
        /// </value>
        bool EnableDelayedProcessing
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether routing is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable route]; otherwise, <c>false</c>.
        /// </value>
        bool EnableRoute
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable status table].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable status table]; otherwise, <c>false</c>.
        /// </value>
        bool EnableStatusTable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the queue.
        /// </summary>
        /// <value>
        /// The type of the queue.
        /// </value>
        QueueTypes QueueType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable message expiration].
        /// </summary>
        /// <value>
        /// <c>true</c> if [enable message expiration]; otherwise, <c>false</c>.
        /// </value>
        bool EnableMessageExpiration
        {
            get;
            set;
        }

        #endregion
    }
    /// <summary>
    /// Types of queues that this transport supports
    /// </summary>
    public enum QueueTypes
    {
        /// <summary>
        /// Standard queue
        /// </summary>
        Normal,
        /// <summary>
        /// RPC send
        /// </summary>
        RpcSend,
        /// <summary>
        /// RPC receive
        /// </summary>
        RpcReceive
    }
}
