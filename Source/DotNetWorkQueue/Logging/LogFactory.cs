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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Creates new instances of <see cref="ILog"/>
    /// </summary>
    public class LogFactory: ILogFactory
    {
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="LogFactory"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        public LogFactory(IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Creates a new instance of <see cref="ILog"/>
        /// </summary>
        /// <remarks>The queue name will be used for the name of the logger</remarks>
        /// <returns></returns>
        public ILog Create()
        {
            return LogProvider.GetLogger(_connectionInformation.QueueName);
        }
        /// <summary>
        /// Creates a new instance of <see cref="ILog" />
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public ILog Create(string name)
        {
            return LogProvider.GetLogger(name);
        }
    }
}
