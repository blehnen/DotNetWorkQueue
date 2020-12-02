// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.Shared.Message;

namespace DotNetWorkQueue.Transport.Shared
{
    /// <summary>
    /// Shared init methods for IoC container
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Configuration.TransportInitDuplex" />
    public class TransportMessageQueueSharedInit : TransportInitDuplex
    {
        /// <summary>
        /// Allows a transport to register its dependencies in the IoC container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType, QueueConnection queueConnection)
        {
            container.Register<ITransportCommitMessage, TransportCommitMessage>(LifeStyles.Singleton);
            container.Register<ITransportHandleMessage, TransportHandleMessage>(LifeStyles.Singleton);
        }
    }
}
