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
        /// <remarks>The expected type is <see cref="Polly.ResiliencePipeline"/>.</remarks>
        public string ReceiveMessageFromTransport => "ReceiveMessageFromTransport";

        /// <summary>
        /// The receive message from transport policy (async)
        /// </summary>
        /// <value>
        /// The receive message from transport.
        /// </value>
        /// <remarks>V8 uses a unified pipeline for sync and async. Returns the same key as <see cref="ReceiveMessageFromTransport"/>.</remarks>
        public string ReceiveMessageFromTransportAsync => "ReceiveMessageFromTransport";

        /// <summary>
        /// Send message command
        /// </summary>
        /// <value>
        /// Send message command
        /// </value>
        /// <remarks>The expected type is <see cref="Polly.ResiliencePipeline"/>.</remarks>
        public string SendMessage => "SendMessage";

        /// <summary>
        /// Send heartbeat command
        /// </summary>
        /// <value>
        /// Send heartbeat command
        /// </value>
        /// <remarks>The expected type is <see cref="Polly.ResiliencePipeline"/>.</remarks>
        public string SendHeartBeat => "SendHeartBeat";

        /// <summary>
        /// Send message command (async)
        /// </summary>
        /// <value>
        /// Send message command
        /// </value>
        /// <remarks>V8 uses a unified pipeline for sync and async. Returns the same key as <see cref="SendMessage"/>.</remarks>
        public string SendMessageAsync => "SendMessage";

        /// <summary>
        /// Send heartbeat command (async)
        /// </summary>
        /// <value>
        /// Send heartbeat command
        /// </value>
        /// <remarks>V8 uses a unified pipeline for sync and async. Returns the same key as <see cref="SendHeartBeat"/>.</remarks>
        public string SendHeartBeatAsync => "SendHeartBeat";
    }
}
