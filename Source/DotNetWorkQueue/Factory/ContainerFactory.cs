// ---------------------------------------------------------------------
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
using System;
using DotNetWorkQueue.IoC;
using SimpleInjector;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates an instance of <seealso cref="IContainer"/> for root factories
    /// </summary>
    /// <remarks><seealso cref="IContainer"/> cannot be injected into most IoC containers, as that's a circular reference. However, root factories
    /// sometimes need access to the container. This allows access to the container without <seealso cref="IContainer"/> being injected</remarks>
    internal class ContainerFactory : IContainerFactory, IDisposable
    {
        private ContainerWrapper _containerWrapper;
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ContainerFactory(Container container)
        {
            _containerWrapper = new ContainerWrapper(container);
        }
        /// <summary>
        /// Creates an instance of <seealso cref="IContainer" />
        /// </summary>
        /// <returns></returns>
        public IContainer Create()
        {
            return _containerWrapper;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _containerWrapper.Dispose();
            _containerWrapper = null;
        }
    }
}
