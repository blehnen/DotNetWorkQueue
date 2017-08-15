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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Serialization;

namespace DotNetWorkQueue
{
    internal static class RegisterConnectionImplementation
    {
        /// <summary>
        /// Registers the implementations for base connection and serialization
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public static void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            container.Register<IConnectionInformation>(() => new BaseConnectionInformation(queue, connection),
                LifeStyles.Singleton);
            container.Register<IInternalSerializer, JsonSerializerInternal>(LifeStyles.Singleton);
            container.Register<IWorkerNotificationFactory, WorkerNotificationFactoryNoOp>(LifeStyles.Singleton);
        }
    }
}
