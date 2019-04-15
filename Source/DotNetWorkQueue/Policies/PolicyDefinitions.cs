// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
namespace DotNetWorkQueue.Policies
{
    /// <summary>
    /// 
    /// </summary>
    public class PolicyDefinitions
    {
        /// <summary>
        /// The receive message from transport policy
        /// </summary>
        /// <value>
        /// The receive message from transport.
        /// </value>
        /// <remarks><seealso cref="Polly.Policy"></seealso> is the expected type.</remarks>
        public string ReceiveMessageFromTransport => "ReceiveMessageFromTransport";

        /// <summary>
        /// The receive message from transport policy
        /// </summary>
        /// <value>
        /// The receive message from transport.
        /// </value>
        /// <remarks><seealso cref="Polly.Policy"></seealso> is the expected type. Policy must be async.</remarks>
        public string ReceiveMessageFromTransportAsync => "ReceiveMessageFromTransportAsync";

        /// <summary>
        /// Send message command
        /// </summary>
        /// <value>
        /// Send message command
        /// </value>
        /// <remarks><seealso cref="Polly.Policy"></seealso> is the expected type</remarks>
        public string SendMessage => "SendMessage";

        /// <summary>
        /// Send heartbeat command
        /// </summary>
        /// <value>
        /// Send heartbeat command
        /// </value>
        /// <remarks><seealso cref="Polly.Policy"></seealso> is the expected type</remarks>
        public string SendHeartBeat => "SendHeartBeat";

        /// <summary>
        /// Send message command
        /// </summary>
        /// <value>
        /// Send message command
        /// </value>
        /// <remarks><seealso cref="Polly.Policy"></seealso> is the expected type. Policy must be async.</remarks>
        public string SendMessageAsync => "SendMessageAsync";

        /// <summary>
        /// Send heartbeat command
        /// </summary>
        /// <value>
        /// Send heartbeat command
        /// </value>
        /// <remarks><seealso cref="Polly.Policy"></seealso> is the expected type. Policy must be async.</remarks>
        public string SendHeartBeatAsync => "SendHeartBeatAsync";
    }
}
