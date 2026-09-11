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
    /// Updates the heart beat for a record
    /// </summary>
    public interface ISendHeartBeat
    {
        /// <summary>
        /// Updates the heart beat for a record.
        /// </summary>
        /// <param name="context">The context.</param>
        IHeartBeatStatus Send(IMessageContext context);

        /// <summary>
        /// Updates the heart beat for a record, without holding a thread while the transport works.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks>
        /// A beat is scheduled work rather than part of processing a message, but it runs on a pool
        /// thread - so a blocking update costs the consumer a thread it could be running messages on,
        /// which is the whole point of the asynchronous consumer.
        /// </remarks>
        Task<IHeartBeatStatus> SendAsync(IMessageContext context);
    }
}
