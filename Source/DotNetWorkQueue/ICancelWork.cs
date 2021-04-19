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
using System.Threading;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Contains the cancel tokens for stopping (don't look for new work) and canceling (stop existing work)
    /// </summary>
    public interface ICancelWork
    {
        /// <summary>
        /// The system is preparing to stop. Don't take on additional work.
        /// </summary>
        /// <value>
        /// The stop work token.
        /// </value>
        CancellationToken StopWorkToken { get;  }
        /// <summary>
        /// The system wants to stop. Cancel any work in progress.
        /// </summary>
        /// <remarks>
        /// Canceling of messages in the middle of processing is up to user code. See <see cref="IWorkerNotification" />
        /// For async processing, messages contained in the in memory queue will be gracefully canceled.
        /// </remarks>
        /// <value>
        /// The cancel work token.
        /// </value>
        CancellationToken CancelWorkToken { get; }
        /// <summary>
        /// All possible cancel tokens. 
        /// </summary>
        /// <code>
        /// You can turn the collection of tokens into a unified cancel token. 
        /// using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ICancelWork.Tokens.ToArray()))
        /// {   
        ///     //combinedCts.Token wraps all of the tokens
        ///     ...
        /// }
        /// </code>
        /// <value>
        /// The tokens.
        /// </value>
        List<CancellationToken> Tokens { get; }
    }
}
