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
using System.Threading.Tasks;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Commits a message to the transport
    /// </summary>
    public interface ICommitMessage
    {
        /// <summary>
        /// Commits the message associated with the message context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        bool Commit(IMessageContext context);

        /// <summary>
        /// Commits the message associated to the context, without blocking a thread.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>true if the message was committed</returns>
        /// <remarks>
        /// The asynchronous consumer commits from a continuation on a thread-pool thread, and a commit
        /// is transport I/O on every message - the busiest of the calls #284 tracks.
        /// </remarks>
        Task<bool> CommitAsync(IMessageContext context);
    }
}
