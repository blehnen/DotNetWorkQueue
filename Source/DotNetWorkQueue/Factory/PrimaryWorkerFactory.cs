// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instance of <see cref="IPrimaryWorker"/>
    /// </summary>
    public class PrimaryWorkerFactory : IPrimaryWorkerFactory
    {
        private readonly IContainerFactory _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryWorkerFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public PrimaryWorkerFactory(IContainerFactory container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }

        /// <summary>
        /// Creates new instance of <see cref="IPrimaryWorker" />
        /// </summary>
        /// <returns></returns>
        public IPrimaryWorker Create()
        {
            return _container.Create().GetInstance<IPrimaryWorker>();
        }
    }
}
