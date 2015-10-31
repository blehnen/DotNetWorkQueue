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
using DotNetWorkQueue.Metrics.Decorator;
namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Returns a new instance of the <see cref="InterceptorFactory"/> class.
    /// </summary>
    internal class InterceptorFactory: IInterceptorFactory
    {
        private readonly IContainerFactory _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="InterceptorFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public InterceptorFactory(IContainerFactory container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }
        /// <summary>
        /// Creates the specified interceptor type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <remarks>The returned type will be wrapped by the message interceptor decorator for capturing metrics</remarks>
        public IMessageInterceptor Create(Type type)
        {
            var interceptor = (IMessageInterceptor)_container.Create().GetInstance(type);
            var decorator = new MessageInterceptorDecorator(_container.Create().GetInstance<IMetrics>(), interceptor, _container.Create().GetInstance<IConnectionInformation>());
            return decorator;
        }
    }
}
