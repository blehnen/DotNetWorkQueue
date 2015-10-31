// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System;
using System.Threading.Tasks;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Registers the callback and type of message for processing
    /// </summary>
    public interface IRegisterMessagesAsync
    {
        /// <summary>
        /// Registers the specified message action.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="messageAction">The message action.</param>
        void Register<TMessage>(Func<IReceivedMessage<TMessage>, IWorkerNotification, Task> messageAction)
           where TMessage : class;


        /// <summary>
        /// Gets a value indicating whether anyc mode has been registered.
        /// </summary>
        /// <value>
        /// <c>true</c> if registered; otherwise, <c>false</c>.
        /// </value>
        bool Registered { get; }
    }
}
