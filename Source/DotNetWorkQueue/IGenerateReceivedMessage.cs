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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Translates the internal <see cref="IReceivedMessageInternal"/> message into the user received message <see cref="IReceivedMessage{T}"/>
    /// </summary>
    /// <remarks>Since we never care what the user type is internally, we use dynamic to avoid having to pass {T} around internally</remarks>
    public interface IGenerateReceivedMessage
    {
        /// <summary>
        /// Generates a <see cref="IReceivedMessage{T}"/> from a <see cref="IReceivedMessageInternal"/>
        /// </summary>
        /// <param name="messageType">Type of the output message.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        dynamic GenerateMessage(Type messageType, IReceivedMessageInternal message);
    }
}
