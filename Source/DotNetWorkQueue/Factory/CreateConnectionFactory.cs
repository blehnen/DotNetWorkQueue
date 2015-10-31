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

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// A factory for creating new singleton connections.
    /// </summary>
    internal class CreateConnectionFactory : ICreateConnectionFactory
    {
        private readonly IContainerFactory _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateConnectionFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public CreateConnectionFactory(IContainerFactory container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }
        /// <summary>
        /// Creates a new instance of <see cref="IConnectionInformation"/>
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public void Register(string queue, string connection)
        {
            var oConnection = _container.Create().GetInstance<IConnectionInformation>();
            oConnection.QueueName = queue;
            oConnection.ConnectionString = connection;
        }
    }
}
