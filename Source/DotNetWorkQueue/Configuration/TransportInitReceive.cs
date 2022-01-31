﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.IoC;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// This class allows a transport to inject its dependencies into the root container.
    /// </summary>
    /// <remarks>This represents a transport that can receive messages.</remarks>
    public abstract class TransportInitReceive : ITransportInitReceive
    {
        /// <summary>
        /// Allows a transport to register its dependencies in the IoC container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        public abstract void RegisterImplementations(IContainer container, RegistrationTypes registrationType, QueueConnection queueConnection);
        /// <summary>
        /// Allows a transport to suppress specific warnings for specific types if needed.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        public virtual void SuppressWarningsIfNeeded(IContainer container, RegistrationTypes registrationType)
        {

        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the requested connection.</param>
        public virtual void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {

        }
    }
}
