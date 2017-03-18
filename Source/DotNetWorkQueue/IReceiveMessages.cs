// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
    /// Interface for receiving messages from a transport.
    /// </summary>
    public interface IReceiveMessages 
    {
        /// <summary>
        /// Returns a message to process.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        IReceivedMessageInternal ReceiveMessage(IMessageContext context);

        /// <summary>
        /// Returns a message to process.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        Task<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context);
    }
}
